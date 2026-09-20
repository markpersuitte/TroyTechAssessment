using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Controllers;

[Authorize(Roles = "Manager")]
public class ReviewController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReviewController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Modal(int applicationId)
    {
        var application = await ManagedSubmittedApplication(applicationId).SingleOrDefaultAsync();
        return application is null
            ? NotFound()
            : PartialView("_ReviewModal", new ReviewInput
            {
                ApplicationId = application.Id,
                ConcurrencyToken = Convert.ToBase64String(application.RowVersion)
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ReviewInput input)
    {
        var outcome = input.Outcome?.Trim();
        if (outcome is not ("Approve" or "Return" or "Deny"))
            ModelState.AddModelError(nameof(input.Outcome), "Select a valid outcome.");
        if (outcome is "Return" or "Deny" && string.IsNullOrWhiteSpace(input.Comment))
            ModelState.AddModelError(nameof(input.Comment), "A comment is required for this outcome.");
        if (outcome is not "Approve" && input.LeaseStartDate.HasValue)
            ModelState.AddModelError(nameof(input.LeaseStartDate),
                "A lease start date can only be provided when approving an application.");
        if (outcome == "Approve" && input.LeaseStartDate is null)
            ModelState.AddModelError(nameof(input.LeaseStartDate), "A lease start date is required.");
        if (input.Comment?.Length > 1000)
            ModelState.AddModelError(nameof(input.Comment), "The comment cannot exceed 1000 characters.");

        var application = await ManagedSubmittedApplication(input.ApplicationId)
            .Include(item => item.Unit)
            .SingleOrDefaultAsync();
        if (application is null) return NotFound();
        if (!SetConcurrencyToken(application, input.ConcurrencyToken))
            return BadRequest("This application is stale. Reload it before reviewing.");
        if (!ModelState.IsValid) return PartialView("_ReviewModal", input);

        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        if (outcome == "Approve" && await HasActiveLeaseAsync(application.UnitId))
        {
            ModelState.AddModelError(string.Empty, "This unit already has an active lease.");
            return PartialView("_ReviewModal", input);
        }

        var statusName = outcome == "Approve" ? "Approved" : outcome == "Return" ? "Returned" : "Denied";
        var status = await _db.ApplicationStatuses.SingleAsync(item => item.Status == statusName);

        if (outcome == "Approve")
        {
            var start = input.LeaseStartDate!.Value.Date;
            if (start < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(input.LeaseStartDate),
                    "The lease start date cannot be in the past.");
                return PartialView("_ReviewModal", input);
            }

            var end = Lease.CalculateEndDate(start);
            if (await HasLeaseOverlapAsync(application.UnitId, start, end))
            {
                ModelState.AddModelError(string.Empty, "This unit already has a lease for that term.");
                return PartialView("_ReviewModal", input);
            }

            _db.Leases.Add(new Lease
            {
                ApplicationId = application.Id,
                UnitId = application.UnitId,
                StartDate = start,
                EndDate = end,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        application.ApplicationStatusId = status.Id;
        application.ClaimedByManagerId = null;
        application.ClaimedAtUtc = null;
        _db.ApplicationStatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            ApplicationStatusId = status.Id,
            ChangedByUserId = CurrentUserId,
            ChangedAtUtc = DateTime.UtcNow,
            Comment = string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim()
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("This application was changed by someone else. Reload it before reviewing.");
        }
        await transaction.CommitAsync();
        return Json(new
        {
            success = true,
            url = Url.Page("/Apply/Apply", new { unitId = application.UnitId, applicationId = application.Id })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNotes(
        int applicationId,
        int? noteId,
        string? notes,
        string? concurrencyToken)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return BadRequest("A manager note is required.");
        if (notes.Length > 4000)
            return BadRequest("Manager notes cannot exceed 4000 characters.");

        var application = await _db.Applications
            .SingleOrDefaultAsync(item =>
                item.Id == applicationId &&
                _db.ManagerProperties.Any(assignment =>
                    assignment.ManagerId == CurrentUserId &&
                    assignment.PropertyId == item.Unit.PropertyId));
        if (application is null)
            return NotFound();

        ManagerNote note;
        if (noteId is int existingNoteId)
        {
            var existingNote = await _db.ManagerNotes.SingleOrDefaultAsync(item =>
                item.Id == existingNoteId &&
                item.ApplicationId == applicationId &&
                item.ManagerId == CurrentUserId);
            if (existingNote is null)
                return NotFound();
            if (!SetNoteConcurrencyToken(existingNote, concurrencyToken))
                return Conflict("This note was changed by someone else. Reload before editing it.");
            note = existingNote;
        }
        else
        {
            note = new ManagerNote
            {
                ApplicationId = applicationId,
                ManagerId = CurrentUserId,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.ManagerNotes.Add(note);
        }

        note.Notes = notes.Trim();
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("This note was changed by someone else. Reload before editing it.");
        }
        return RedirectToPage("/Apply/Apply", new
        {
            unitId = application.UnitId,
            applicationId = application.Id
        });
    }

    private IQueryable<Application> ManagedSubmittedApplication(int applicationId) =>
        _db.Applications.Where(application =>
            application.Id == applicationId &&
            application.ApplicationStatus.Status == "Submitted" &&
            application.ClaimedByManagerId == CurrentUserId &&
            _db.ManagerProperties.Any(assignment =>
                assignment.ManagerId == CurrentUserId &&
                assignment.PropertyId == application.Unit.PropertyId));

    private Task<bool> HasActiveLeaseAsync(int unitId)
    {
        var today = DateTime.UtcNow.Date;
        return _db.Leases.AnyAsync(lease =>
            lease.UnitId == unitId && lease.StartDate <= today && lease.EndDate >= today);
    }

    private Task<bool> HasLeaseOverlapAsync(int unitId, DateTime startDate, DateTime endDate) =>
        _db.Leases.AnyAsync(lease =>
            lease.UnitId == unitId &&
            lease.StartDate <= endDate &&
            lease.EndDate >= startDate);

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    private bool SetConcurrencyToken(Application application, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            _db.Entry(application).Property(item => item.RowVersion).OriginalValue =
                Convert.FromBase64String(token);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private bool SetNoteConcurrencyToken(ManagerNote note, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            _db.Entry(note).Property(item => item.RowVersion).OriginalValue =
                Convert.FromBase64String(token);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public sealed class ReviewInput
    {
        public int ApplicationId { get; set; }
        public string? Outcome { get; set; }
        public string? Comment { get; set; }
        public DateTime? LeaseStartDate { get; set; }
        public string? ConcurrencyToken { get; set; }
    }
}

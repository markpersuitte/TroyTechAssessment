using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Controllers;

[Authorize(Roles = "Applicant")]
public class ResidenceController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Modal(int applicationId, int? residenceId)
    {
        var application = await EditableApplicationQuery(applicationId).SingleOrDefaultAsync();
        if (application is null)
            return NotFound();

        if (residenceId is null)
            return PartialView("_ResidenceModal", new ResidenceInput
            {
                ApplicationId = applicationId,
                ConcurrencyToken = Convert.ToBase64String(application.RowVersion)
            });

        var residence = application.ApplicationResidenceHistory
            .SingleOrDefault(item => item.Id == residenceId.Value);
        return residence is null
            ? NotFound()
            : PartialView("_ResidenceModal", ResidenceInput.From(residence, application.RowVersion));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ResidenceInput input)
    {
        Validate(input);
        var application = await EditableApplicationQuery(input.ApplicationId).SingleOrDefaultAsync();
        if (application is null)
            return NotFound();

        if (input.Id is not null &&
            application.ApplicationResidenceHistory.All(item => item.Id != input.Id.Value))
        {
            return NotFound();
        }

        ValidateDateSequence(input, application);
        if (!ModelState.IsValid)
            return BadRequest(PartialView("_ResidenceModal", input));
        if (!SetConcurrencyToken(application, input.ConcurrencyToken))
            return Conflict("This application was changed by someone else. Reload before editing residence history.");

        ApplicationResidenceHistory residence;
        if (input.Id is null)
        {
            residence = new ApplicationResidenceHistory
            {
                ApplicationId = application.Id
            };
            db.ApplicationResidenceHistory.Add(residence);
        }
        else
        {
            residence = application.ApplicationResidenceHistory
                .Single(item => item.Id == input.Id.Value);
        }

        residence.StreetAddress = input.StreetAddress.Trim();
        residence.City = input.City.Trim();
        residence.State = input.State.Trim();
        residence.ZipCode = input.ZipCode.Trim();
        residence.LandlordFirstName = input.LandlordFirstName.Trim();
        residence.LandlordLastName = input.LandlordLastName.Trim();
        residence.LandlordPhoneNumber = input.LandlordPhoneNumber.Trim();
        residence.MoveInDate = input.MoveInDate!.Value.Date;
        residence.MoveOutDate = input.MoveOutDate?.Date;

        application.LastModifiedAtUtc = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("This application was changed by someone else. Reload before editing residence history.");
        }
        return Json(new
        {
            success = true,
            url = Url.Page("/Apply/Apply", new
            {
                unitId = application.UnitId,
                applicationId = application.Id,
                section = "Residence"
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int applicationId, int residenceId)
    {
        var application = await EditableApplicationQuery(applicationId).SingleOrDefaultAsync();
        if (application is null)
            return NotFound();

        var residence = application.ApplicationResidenceHistory
            .SingleOrDefault(item => item.Id == residenceId);
        if (residence is null)
            return NotFound();
        if (!SetConcurrencyToken(application, Request.Headers["X-Concurrency-Token"]))
            return Conflict("This application was changed by someone else. Reload before editing residence history.");

        db.ApplicationResidenceHistory.Remove(residence);
        application.LastModifiedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Json(new
        {
            success = true,
            url = Url.Page("/Apply/Apply", new
            {
                unitId = application.UnitId,
                applicationId = application.Id,
                section = "Residence"
            })
        });
    }

    private IQueryable<Application> EditableApplicationQuery(int applicationId)
    {
        if (!User.IsInRole("Applicant") || User.IsInRole("Manager"))
        {
            return db.Applications.Where(application => false);
        }

        var userId = CurrentUserId;
        return db.Applications
            .Include(application => application.ApplicationResidenceHistory)
            .Where(application =>
                application.Id == applicationId &&
                application.UserId == userId &&
                (application.ApplicationStatusId == 1 ||
                 application.ApplicationStatusId == 3));
    }

    private void Validate(ResidenceInput input)
    {
        if (string.IsNullOrWhiteSpace(input.StreetAddress))
            ModelState.AddModelError(nameof(input.StreetAddress), "Street address is required.");
        if (string.IsNullOrWhiteSpace(input.City))
            ModelState.AddModelError(nameof(input.City), "City is required.");
        if (string.IsNullOrWhiteSpace(input.State))
            ModelState.AddModelError(nameof(input.State), "State is required.");
        if (string.IsNullOrWhiteSpace(input.ZipCode))
            ModelState.AddModelError(nameof(input.ZipCode), "ZIP code is required.");
        if (string.IsNullOrWhiteSpace(input.LandlordFirstName))
            ModelState.AddModelError(nameof(input.LandlordFirstName), "Landlord first name is required.");
        if (string.IsNullOrWhiteSpace(input.LandlordLastName))
            ModelState.AddModelError(nameof(input.LandlordLastName), "Landlord last name is required.");
        if (string.IsNullOrWhiteSpace(input.LandlordPhoneNumber))
            ModelState.AddModelError(nameof(input.LandlordPhoneNumber), "Landlord phone is required.");
        if (input.MoveInDate is null)
            ModelState.AddModelError(nameof(input.MoveInDate), "Move-in date is required.");
        else if (input.MoveInDate.Value.Date > DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(input.MoveInDate), "Move-in date cannot be in the future.");
        if (input.MoveOutDate.HasValue && input.MoveInDate.HasValue &&
            !ApplicationResidenceHistory.HasValidDateRange(
                input.MoveInDate.Value,
                input.MoveOutDate.Value))
            ModelState.AddModelError(nameof(input.MoveOutDate), "Move-out date cannot be before move-in date.");
    }

    private void ValidateDateSequence(
        ResidenceInput input,
        Application application)
    {
        if (input.MoveInDate is null)
            return;

        var residences = application.ApplicationResidenceHistory
            .Where(item => input.Id != item.Id)
            .Select(item => new DateRange(item.MoveInDate, item.MoveOutDate))
            .Append(new DateRange(input.MoveInDate.Value, input.MoveOutDate))
            .OrderByDescending(item => item.MoveInDate)
            .ToList();

        for (var index = 0; index < residences.Count; index++)
        {
            if (residences[index].MoveOutDate is null && index > 0)
            {
                ModelState.AddModelError(nameof(input.MoveOutDate),
                    "Only the most recent residence may have no move-out date.");
            }

            if (index == 0)
                continue;

            var newer = residences[index - 1];
            var older = residences[index];
            if (older.MoveOutDate is null ||
                older.MoveOutDate.Value.Date > newer.MoveInDate.Date)
            {
                ModelState.AddModelError(nameof(input.MoveOutDate),
                    "Residence dates cannot overlap and must be entered from most recent to oldest.");
            }
        }
    }

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id)
        ? id
        : Guid.Empty;

    private bool SetConcurrencyToken(Application application, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;
        try
        {
            db.Entry(application).Property(item => item.RowVersion).OriginalValue =
                Convert.FromBase64String(token);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public sealed class ResidenceInput
    {
        public int? Id { get; init; }
        public int ApplicationId { get; init; }
        [Required, StringLength(100)]
        [Display(Name = "Street address")]
        public string StreetAddress { get; init; } = string.Empty;
        [Required, StringLength(50)]
        public string City { get; init; } = string.Empty;
        [Required, StringLength(50)]
        public string State { get; init; } = string.Empty;
        [Required, StringLength(10)]
        [Display(Name = "ZIP code")]
        public string ZipCode { get; init; } = string.Empty;
        [Required, StringLength(50)]
        [Display(Name = "Landlord first name")]
        public string LandlordFirstName { get; init; } = string.Empty;
        [Required, StringLength(50)]
        [Display(Name = "Landlord last name")]
        public string LandlordLastName { get; init; } = string.Empty;
        [Required, StringLength(50)]
        [Display(Name = "Landlord phone number")]
        public string LandlordPhoneNumber { get; init; } = string.Empty;
        [Required, DataType(DataType.Date)]
        [Display(Name = "Move-in date")]
        public DateTime? MoveInDate { get; init; }
        [DataType(DataType.Date)]
        [Display(Name = "Move-out date")]
        public DateTime? MoveOutDate { get; init; }
        public string? ConcurrencyToken { get; init; }

        public static ResidenceInput From(ApplicationResidenceHistory residence, byte[] rowVersion) => new()
        {
            Id = residence.Id,
            ApplicationId = residence.ApplicationId,
            StreetAddress = residence.StreetAddress,
            City = residence.City,
            State = residence.State,
            ZipCode = residence.ZipCode,
            LandlordFirstName = residence.LandlordFirstName,
            LandlordLastName = residence.LandlordLastName,
            LandlordPhoneNumber = residence.LandlordPhoneNumber,
            MoveInDate = residence.MoveInDate,
            MoveOutDate = residence.MoveOutDate
            ,ConcurrencyToken = Convert.ToBase64String(rowVersion)
        };
    }

    private sealed record DateRange(DateTime MoveInDate, DateTime? MoveOutDate);
}

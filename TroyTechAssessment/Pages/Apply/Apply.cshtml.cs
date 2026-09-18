using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Pages;

[Authorize(Roles = "Applicant,Manager")]
public class ApplyModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public ApplyModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public Unit Unit { get; private set; } = null!;
    public bool IsReadOnly { get; private set; }
    public string? ApplicationStatus { get; private set; }
    public IList<ManagerNote> ManagerNotes { get; private set; } = [];
    public Guid CurrentUserId { get; private set; }
    public Guid? ClaimedByManagerId { get; private set; }
    public IList<ApplicationStatusHistory> StatusHistory { get; private set; } = [];

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int unitId, int? applicationId, string? section = null)
    {
        var isApplicant = User.IsInRole("Applicant");
        var isManager = User.IsInRole("Manager");
        if (isApplicant == isManager)
        {
            return Forbid();
        }

        var unit = await LoadUnitAsync(unitId);
        if (unit is null)
        {
            return NotFound();
        }

        if (applicationId is null && isApplicant &&
            await HasActiveLeaseAsync(unitId))
        {
            return BadRequest("This unit is not currently available.");
        }

        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var hasUserId = Guid.TryParse(userIdValue, out var userId);
        CurrentUserId = userId;
        if (applicationId is null && isApplicant &&
            hasUserId &&
            await FindExistingApplicationAsync(unitId, userId) is { } existingApplication)
        {
            return RedirectToPage("/Apply/Apply", new
            {
                unitId,
                applicationId = existingApplication.Id
            });
        }

        Unit = unit;
        Input.UnitId = unit.Id;
        Input.ApplicationId = applicationId;
        var user = !hasUserId
            ? null
            : await _db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId);
        Input.Email = user?.Email ?? User.Identity?.Name ?? string.Empty;
        Input.FirstName = user?.FirstName ?? string.Empty;
        Input.LastName = user?.LastName ?? string.Empty;
        if (applicationId is int existingApplicationId)
        {
            var application = await _db.Applications
                .Include(item => item.ApplicationResidenceHistory)
                .Include(item => item.ApplicationStatus)
                .Include(item => item.StatusHistory)
                    .ThenInclude(history => history.ApplicationStatus)
                .Include(item => item.StatusHistory)
                    .ThenInclude(history => history.ChangedByUser)
                .Include(item => item.ManagerNotes)
                    .ThenInclude(note => note.Manager)
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == existingApplicationId &&
                    item.UnitId == unitId &&
                    (isManager
                        ? _db.ManagerProperties.Any(assignment =>
                            assignment.ManagerId == userId &&
                            assignment.PropertyId == item.Unit.PropertyId)
                        : item.UserId == userId));
            if (application is null)
            {
                return NotFound();
            }

            IsReadOnly = !IsEditableStatus(application.ApplicationStatusId);
            ApplicationStatus = application.ApplicationStatus.Status;
            ClaimedByManagerId = application.ClaimedByManagerId;
            Input.ConcurrencyToken = Convert.ToBase64String(application.RowVersion);
            if (isManager)
                ManagerNotes = application.ManagerNotes
                    .OrderBy(note => note.CreatedAtUtc)
                    .ToList();
            StatusHistory = application.StatusHistory
                .OrderByDescending(history => history.ChangedAtUtc)
                .ToList();
            Input.FirstName = application.FirstName;
            Input.LastName = application.LastName;
            Input.Email = application.Email;
            Input.PhoneNumber = application.PhoneNumber;
            Input.CurrentStreetAddress = application.CurrentStreetAddress;
            Input.CurrentCity = application.CurrentCity;
            Input.CurrentState = application.CurrentState;
            Input.CurrentZipCode = application.CurrentZipCode;
            Input.ResidenceHistory = ToResidenceInputs(application);
            if (!string.IsNullOrWhiteSpace(application.DraftResidenceHistoryJson))
            {
                Input.ResidenceHistory = JsonSerializer.Deserialize<List<ResidenceHistoryInput>>(
                    application.DraftResidenceHistoryJson) ?? Input.ResidenceHistory;
            }
        }
        else
        {
            Input.ResidenceHistory =
            [
                new ResidenceHistoryInput()
            ];
        }
        Input.CurrentSection = section is "Application" or "Residence" or "Summary"
            ? section
            : "Application";
        return Page();
    }

    public Task<IActionResult> OnPostWithdrawAsync(int unitId, int applicationId)
    {
        return ChangeStatusAsync(unitId, applicationId, "Withdrawn", "Applicant withdrew the application.", "Applicant");
    }

    public async Task<IActionResult> OnPostClaimAsync(int unitId, int applicationId)
    {
        if (!User.IsInRole("Manager") || !TryGetCurrentUserId(out var managerId))
            return Forbid();

        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        var application = await _db.Applications
            .Include(item => item.Unit)
            .SingleOrDefaultAsync(item =>
                item.Id == applicationId &&
                item.UnitId == unitId &&
                item.ApplicationStatusId == 2 &&
                item.ClaimedByManagerId == null &&
                _db.ManagerProperties.Any(assignment =>
                    assignment.ManagerId == managerId &&
                    assignment.PropertyId == item.Unit.PropertyId));
        if (application is null)
            return NotFound();

        application.ClaimedByManagerId = managerId;
        application.ClaimedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToPage("/Apply/Apply", new { unitId, applicationId });
    }

    public async Task<IActionResult> OnPostReleaseAsync(int unitId, int applicationId)
    {
        if (!User.IsInRole("Manager") || !TryGetCurrentUserId(out var managerId))
            return Forbid();

        var application = await _db.Applications
            .Include(item => item.Unit)
            .SingleOrDefaultAsync(item =>
                item.Id == applicationId &&
                item.UnitId == unitId &&
                item.ApplicationStatusId == 2 &&
                item.ClaimedByManagerId == managerId &&
                _db.ManagerProperties.Any(assignment =>
                    assignment.ManagerId == managerId &&
                    assignment.PropertyId == item.Unit.PropertyId));
        if (application is null)
            return NotFound();

        application.ClaimedByManagerId = null;
        application.ClaimedAtUtc = null;
        await _db.SaveChangesAsync();

        return RedirectToPage("/Apply/Apply", new { unitId, applicationId });
    }

    public Task<IActionResult> OnPostReviewAsync(
        int unitId,
        int applicationId,
        string outcome,
        string? reviewComment,
        DateTime? leaseStartDate)
    {
        var targetStatus = outcome switch
        {
            "Approve" => "Approved",
            "Return" => "Returned",
            "Deny" => "Denied",
            _ => string.Empty
        };

        if (targetStatus == string.Empty)
        {
            return Task.FromResult<IActionResult>(BadRequest("Select a valid review outcome."));
        }

        return ChangeStatusAsync(
            unitId,
            applicationId,
            targetStatus,
            reviewComment?.Trim() ?? string.Empty,
            "Manager",
            leaseStartDate);
    }

    public Task<IActionResult> OnPostReturnAsync(int unitId, int applicationId)
    {
        return ChangeStatusAsync(unitId, applicationId, "Returned",
            Input.ReviewComment ?? "Application returned for additional information.", "Manager");
    }

    public Task<IActionResult> OnPostDenyAsync(int unitId, int applicationId)
    {
        return ChangeStatusAsync(unitId, applicationId, "Denied",
            Input.ReviewComment ?? "Application denied.", "Manager");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!User.IsInRole("Applicant") || User.IsInRole("Manager"))
        {
            return Forbid();
        }

        var unit = await LoadUnitAsync(Input.UnitId);
        if (unit is null)
        {
            return NotFound();
        }

        Unit = unit;
        var application = await LoadEditableApplicationAsync();
        if (Input.ApplicationId is not null && application is null)
        {
            return NotFound();
        }

        if (application is not null)
        {
            if (!SetConcurrencyToken(application))
                return StaleSave();
            Input.ApplicationId = application.Id;
            ApplicationStatus = application.ApplicationStatus.Status;
            IsReadOnly = !IsEditableStatus(application.ApplicationStatusId);
            StatusHistory = application.StatusHistory
                .OrderByDescending(history => history.ChangedAtUtc)
                .ToList();
            Input.ResidenceHistory = ToResidenceInputs(application);
        }

        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var hasUserId = Guid.TryParse(userIdValue, out var userId);
        var user = !hasUserId
            ? null
            : await _db.Users.SingleOrDefaultAsync(item => item.Id == userId);
        if (user is null)
        {
            return Forbid();
        }

        if (application is null && Input.ApplicationId is null &&
            await FindExistingApplicationAsync(unit.Id, user.Id) is { } existingApplication)
        {
            application = existingApplication;
            Input.ApplicationId = application.Id;
            ApplicationStatus = application.ApplicationStatus.Status;
            IsReadOnly = !IsEditableStatus(application.ApplicationStatusId);
            StatusHistory = application.StatusHistory
                .OrderByDescending(history => history.ChangedAtUtc)
                .ToList();
            Input.ResidenceHistory = ToResidenceInputs(application);
        }

        if (application is not null && application.ApplicationStatusId is not (1 or 3))
        {
            return Forbid();
        }

        switch (Input.Action)
        {
            case "ContinueApplication":
                ModelState.Clear();
                ValidateApplicantInformation();
                Input.CurrentSection = "Application";
                if (!ModelState.IsValid)
                    return Page();

                application ??= CreateApplication(user.Id, unit.Id);
                if (!SetConcurrencyToken(application))
                    return StaleSave();
                SaveApplicantInformation(application);
                if (!await TrySaveChangesAsync(application))
                    return StaleSave();
                Input.ConcurrencyToken = Convert.ToBase64String(application.RowVersion);
                Input.ApplicationId = application.Id;
                Input.CurrentSection = "Residence";
                return Page();

            case "ContinueResidence":
                ModelState.Clear();
                if (application is null)
                {
                    ModelState.AddModelError(string.Empty, "Save Applicant Information before continuing.");
                    Input.CurrentSection = "Application";
                    return Page();
                }

                ValidateResidenceHistory();
                Input.CurrentSection = "Residence";
                if (!ModelState.IsValid)
                    return Page();

                if (!SetConcurrencyToken(application))
                    return StaleSave();
                SaveResidenceHistory(application);
                if (!await TrySaveChangesAsync(application))
                    return StaleSave();
                Input.ConcurrencyToken = Convert.ToBase64String(application.RowVersion);
                Input.CurrentSection = "Summary";
                return Page();

            case "BackToApplication":
                Input.CurrentSection = "Application";
                return Page();

            case "BackToResidence":
                Input.CurrentSection = "Residence";
                return Page();

            case "SaveDraft":
                ModelState.Clear();
                ValidateApplicantInformation();
                ValidateResidenceHistory();
                application ??= CreateApplication(user.Id, unit.Id);
                if (!SetConcurrencyToken(application))
                    return StaleSave();
                SaveApplicantInformation(application);
                SaveDraftResidenceHistory(application);
                if (!await TrySaveChangesAsync(application))
                    return StaleSave();
                return Page();

            case "Submit":
                ModelState.Clear();
                ValidateApplicantInformation();
                ValidateResidenceHistory();
                if (application is null)
                {
                    ModelState.AddModelError(string.Empty, "Complete both sections before submitting.");
                }

                if (application is not null && await HasActiveLeaseAsync(unit.Id))
                {
                    ModelState.AddModelError(string.Empty, "This unit is no longer available.");
                }

                Input.CurrentSection = "Summary";
                if (!ModelState.IsValid)
                    return Page();

                await using (var transaction = await _db.Database.BeginTransactionAsync())
                {
                    if (!SetConcurrencyToken(application!))
                        return StaleSave();
                    SaveApplicantInformation(application!);
                    SaveResidenceHistory(application!);
                    application!.DraftResidenceHistoryJson = null;
                    await RecordStatusChangeAsync(application!, 2, user.Id, "Application submitted.");
                    if (!await TrySaveChangesAsync(application!))
                        return StaleSave();
                    await transaction.CommitAsync();
                }

                return RedirectToPage("/Index");

            default:
                return BadRequest("Select a valid application action.");
        }
    }

    private async Task<Application?> LoadEditableApplicationAsync()
    {
        if (Input.ApplicationId is not int applicationId)
            return null;

        var userId = Guid.TryParse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            out var parsedUserId)
            ? parsedUserId
            : Guid.Empty;

        return await _db.Applications
            .Include(item => item.ApplicationResidenceHistory)
            .Include(item => item.ApplicationStatus)
            .Include(item => item.StatusHistory)
                .ThenInclude(history => history.ApplicationStatus)
            .Include(item => item.StatusHistory)
                .ThenInclude(history => history.ChangedByUser)
            .SingleOrDefaultAsync(item => item.Id == applicationId &&
                item.UnitId == Input.UnitId &&
                item.UserId == userId);
    }

    private bool SetConcurrencyToken(Application application)
    {
        if (application.Id == 0)
            return true;

        if (string.IsNullOrWhiteSpace(Input.ConcurrencyToken))
        {
            ModelState.AddModelError(string.Empty,
                "This application is stale. Reload it before saving your changes.");
            return false;
        }

        try
        {
            var originalToken = Convert.FromBase64String(Input.ConcurrencyToken);
            _db.Entry(application).Property(item => item.RowVersion).OriginalValue = originalToken;
            return true;
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty,
                "This application is stale. Reload it before saving your changes.");
            return false;
        }
    }

    private async Task<bool> TrySaveChangesAsync(Application application)
    {
        try
        {
            await _db.SaveChangesAsync();
            Input.ConcurrencyToken = Convert.ToBase64String(application.RowVersion);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty,
                "This application was changed by someone else. Reload it before saving your changes.");
            return false;
        }
    }

    private IActionResult StaleSave()
    {
        Input.CurrentSection = Input.CurrentSection is "Application" or "Residence" or "Summary"
            ? Input.CurrentSection
            : "Application";
        return Page();
    }

    private Application CreateApplication(Guid userId, int unitId)
    {
        var application = new Application
        {
            UserId = userId,
            UnitId = unitId,
            ApplicationStatusId = 1
        };
        _db.Applications.Add(application);
        _db.ApplicationStatusHistory.Add(new ApplicationStatusHistory
        {
            Application = application,
            ApplicationStatusId = 1,
            ChangedByUserId = userId,
            ChangedAtUtc = DateTime.UtcNow,
            Comment = "Application started."
        });
        return application;
    }

    private void SaveApplicantInformation(Application application)
    {
        application.LastModifiedAtUtc = DateTime.UtcNow;
        application.FirstName = Input.FirstName?.Trim() ?? string.Empty;
        application.LastName = Input.LastName?.Trim() ?? string.Empty;
        application.Email = Input.Email?.Trim() ?? string.Empty;
        application.PhoneNumber = Input.PhoneNumber?.Trim() ?? string.Empty;
        application.CurrentStreetAddress = Input.CurrentStreetAddress?.Trim() ?? string.Empty;
        application.CurrentCity = Input.CurrentCity?.Trim() ?? string.Empty;
        application.CurrentState = Input.CurrentState?.Trim() ?? string.Empty;
        application.CurrentZipCode = Input.CurrentZipCode?.Trim() ?? string.Empty;
    }

    private void SaveResidenceHistory(Application application, bool allowIncomplete = false)
    {
        application.LastModifiedAtUtc = DateTime.UtcNow;
        application.DraftResidenceHistoryJson = null;
        _db.ApplicationResidenceHistory.RemoveRange(application.ApplicationResidenceHistory);
        application.ApplicationResidenceHistory.Clear();
        var residences = allowIncomplete
            ? Input.ResidenceHistory.Where(HasCompleteResidenceData)
            : Input.ResidenceHistory;
        foreach (var residence in residences)
        {
            _db.ApplicationResidenceHistory.Add(new ApplicationResidenceHistory
            {
                ApplicationId = application.Id,
                StreetAddress = residence.StreetAddress.Trim(),
                City = residence.City.Trim(),
                State = residence.State.Trim(),
                ZipCode = residence.ZipCode.Trim(),
                LandlordFirstName = residence.LandlordFirstName.Trim(),
                LandlordLastName = residence.LandlordLastName.Trim(),
                LandlordPhoneNumber = residence.LandlordPhoneNumber.Trim(),
                MoveInDate = residence.MoveInDate!.Value,
                MoveOutDate = residence.MoveOutDate
            });
        }
    }

    private void SaveDraftResidenceHistory(Application application)
    {
        application.LastModifiedAtUtc = DateTime.UtcNow;
        application.DraftResidenceHistoryJson = JsonSerializer.Serialize(Input.ResidenceHistory);
        var completeResidences = Input.ResidenceHistory.Where(HasCompleteResidenceData).ToList();
        if (completeResidences.Count > 0)
        {
            SaveResidenceHistory(application, allowIncomplete: true);
            application.DraftResidenceHistoryJson = JsonSerializer.Serialize(Input.ResidenceHistory);
        }
    }

    private static List<ResidenceHistoryInput> ToResidenceInputs(Application application) =>
        application.ApplicationResidenceHistory
            .OrderByDescending(history => history.MoveInDate)
            .ThenByDescending(history => history.Id)
            .Select(history => new ResidenceHistoryInput
            {
                Id = history.Id,
                StreetAddress = history.StreetAddress,
                City = history.City,
                State = history.State,
                ZipCode = history.ZipCode,
                LandlordFirstName = history.LandlordFirstName,
                LandlordLastName = history.LandlordLastName,
                LandlordPhoneNumber = history.LandlordPhoneNumber,
                MoveInDate = history.MoveInDate,
                MoveOutDate = history.MoveOutDate
            })
            .ToList();

    private void ValidateApplicantInformation()
    {
        if (string.IsNullOrWhiteSpace(Input.FirstName))
            ModelState.AddModelError(nameof(Input.FirstName), "First name is required.");
        if (string.IsNullOrWhiteSpace(Input.LastName))
            ModelState.AddModelError(nameof(Input.LastName), "Last name is required.");
        if (string.IsNullOrWhiteSpace(Input.Email) ||
            !new EmailAddressAttribute().IsValid(Input.Email))
            ModelState.AddModelError(nameof(Input.Email), "Enter a valid email address.");
        if (string.IsNullOrWhiteSpace(Input.PhoneNumber))
            ModelState.AddModelError(nameof(Input.PhoneNumber), "Phone number is required.");
        if (string.IsNullOrWhiteSpace(Input.CurrentStreetAddress))
            ModelState.AddModelError(nameof(Input.CurrentStreetAddress), "Street address is required.");
        if (string.IsNullOrWhiteSpace(Input.CurrentCity))
            ModelState.AddModelError(nameof(Input.CurrentCity), "City is required.");
        if (string.IsNullOrWhiteSpace(Input.CurrentState))
            ModelState.AddModelError(nameof(Input.CurrentState), "State is required.");
        if (string.IsNullOrWhiteSpace(Input.CurrentZipCode))
            ModelState.AddModelError(nameof(Input.CurrentZipCode), "ZIP code is required.");
    }

    private async Task<IActionResult> ChangeStatusAsync(
        int unitId,
        int applicationId,
        string targetStatus,
        string comment,
        string requiredRole,
        DateTime? leaseStartDate = null)
    {
        if (!User.IsInRole(requiredRole))
        {
            return Forbid();
        }

        if ((targetStatus is "Returned" or "Denied") && string.IsNullOrWhiteSpace(comment))
        {
            return BadRequest("A comment is required when returning or denying an application.");
        }

        if (comment.Length > 1000)
        {
            return BadRequest("The review comment cannot exceed 1000 characters.");
        }

        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Forbid();
        }

        var application = await _db.Applications
            .Include(item => item.Unit)
            .SingleOrDefaultAsync(item => item.Id == applicationId &&
                item.UnitId == unitId &&
                (requiredRole == "Manager"
                    ? _db.ManagerProperties.Any(assignment =>
                        assignment.ManagerId == userId &&
                        assignment.PropertyId == item.Unit.PropertyId) &&
                      item.ClaimedByManagerId == userId
                    : item.UserId == userId));
        if (application is null)
        {
            return NotFound();
        }

        if (application.ApplicationStatusId != 2)
        {
            return BadRequest("Only submitted applications can change status.");
        }

        if (targetStatus == "Approved" && await HasActiveLeaseAsync(unitId))
        {
            return BadRequest("This unit already has an active lease.");
        }

        if (targetStatus == "Approved" &&
            (!leaseStartDate.HasValue || leaseStartDate.Value == default))
        {
            return BadRequest("A lease start date is required.");
        }

        if (targetStatus == "Approved" && leaseStartDate!.Value.Date < DateTime.UtcNow.Date)
        {
            return BadRequest("The lease start date cannot be in the past.");
        }

        var status = await _db.ApplicationStatuses
            .SingleOrDefaultAsync(item => item.Status == targetStatus);
        if (status is null)
        {
            return BadRequest("The requested application status is not configured.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        if (targetStatus == "Approved")
        {
            var startDate = leaseStartDate!.Value.Date;
            var endDate = Lease.CalculateEndDate(startDate);
            if (await HasLeaseOverlapAsync(unitId, startDate, endDate))
            {
                return BadRequest("This unit already has a lease for that term.");
            }

            _db.Leases.Add(new Lease
            {
                UnitId = unitId,
                ApplicationId = application.Id,
                StartDate = startDate,
                EndDate = endDate,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await RecordStatusChangeAsync(application, status.Id, userId, comment);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return RedirectToPage("/Apply/Apply", new { unitId, applicationId });
    }

    private static bool IsEditableStatus(int applicationStatusId) =>
        applicationStatusId is 1 or 3;

    private bool TryGetCurrentUserId(out Guid userId) =>
        Guid.TryParse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            out userId);

    private async Task RecordStatusChangeAsync(Application application, int applicationStatusId, Guid changedByUserId, string? comment)
    {
        application.ApplicationStatusId = applicationStatusId;
        application.ClaimedByManagerId = null;
        application.ClaimedAtUtc = null;
        _db.ApplicationStatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            ApplicationStatusId = applicationStatusId,
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = DateTime.UtcNow,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
        });
    }

    private Task<bool> HasLeaseOverlapAsync(int unitId, DateTime startDate, DateTime endDate) =>
        _db.Leases.AnyAsync(lease =>
            lease.UnitId == unitId &&
            lease.StartDate <= endDate &&
            lease.EndDate >= startDate);

    private Task<Unit?> LoadUnitAsync(int unitId)
    {
        var managerId = Guid.TryParse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            out var parsedManagerId)
            ? parsedManagerId
            : Guid.Empty;

        return _db.Units
            .Include(unit => unit.Property)
            .Include(unit => unit.UnitType)
            .AsNoTracking()
            .SingleOrDefaultAsync(unit => unit.Id == unitId &&
                (!User.IsInRole("Manager") ||
                 _db.ManagerProperties.Any(assignment =>
                     assignment.ManagerId == managerId &&
                     assignment.PropertyId == unit.PropertyId)));
    }

    private Task<bool> HasActiveLeaseAsync(int unitId)
    {
        var today = DateTime.UtcNow.Date;
        return _db.Leases.AsNoTracking().AnyAsync(lease =>
            lease.UnitId == unitId &&
            lease.StartDate <= today &&
            lease.EndDate >= today);
    }

    public class InputModel
    {
        public string Action { get; set; } = string.Empty;
        public string CurrentSection { get; set; } = "Application";
        public string? ReviewComment { get; set; }
        public int? ApplicationId { get; set; }
        public string? ConcurrencyToken { get; set; }
        public int UnitId { get; set; }
        public List<int> ResidenceIds { get; set; } = [];

        [Required(ErrorMessage = "First name is required."), StringLength(50, ErrorMessage = "First name must be 50 characters or fewer.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required."), StringLength(50, ErrorMessage = "Last name must be 50 characters or fewer.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required."), EmailAddress(ErrorMessage = "Enter a valid email address, such as name@example.com."), StringLength(256, ErrorMessage = "Email address must be 256 characters or fewer.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required."), Phone(ErrorMessage = "Enter a valid phone number."), StringLength(25, ErrorMessage = "Phone number must be 25 characters or fewer.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Street address is required."), StringLength(100, ErrorMessage = "Street address must be 100 characters or fewer.")]
        public string CurrentStreetAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required."), StringLength(50, ErrorMessage = "City must be 50 characters or fewer.")]
        public string CurrentCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required."), StringLength(50, ErrorMessage = "State must be 50 characters or fewer.")]
        public string CurrentState { get; set; } = string.Empty;

        [Required(ErrorMessage = "ZIP code is required."), StringLength(10, ErrorMessage = "ZIP code must be 10 characters or fewer.")]
        public string CurrentZipCode { get; set; } = string.Empty;

        [MinLength(1)]
        public List<ResidenceHistoryInput> ResidenceHistory { get; set; } = [];
    }

    private static bool HasResidenceData(ResidenceHistoryInput residence)
    {
        return !string.IsNullOrWhiteSpace(residence.StreetAddress) ||
               !string.IsNullOrWhiteSpace(residence.City) ||
               !string.IsNullOrWhiteSpace(residence.State) ||
               !string.IsNullOrWhiteSpace(residence.ZipCode) ||
               !string.IsNullOrWhiteSpace(residence.LandlordFirstName) ||
               !string.IsNullOrWhiteSpace(residence.LandlordLastName) ||
               !string.IsNullOrWhiteSpace(residence.LandlordPhoneNumber) ||
               residence.MoveInDate.HasValue ||
               residence.MoveOutDate.HasValue;
    }

    private static bool HasCompleteResidenceData(ResidenceHistoryInput residence)
    {
        return HasResidenceData(residence) &&
               !string.IsNullOrWhiteSpace(residence.StreetAddress) &&
               !string.IsNullOrWhiteSpace(residence.City) &&
               !string.IsNullOrWhiteSpace(residence.State) &&
               !string.IsNullOrWhiteSpace(residence.ZipCode) &&
               !string.IsNullOrWhiteSpace(residence.LandlordFirstName) &&
               !string.IsNullOrWhiteSpace(residence.LandlordLastName) &&
               !string.IsNullOrWhiteSpace(residence.LandlordPhoneNumber) &&
               residence.MoveInDate.HasValue;
    }

    public class ResidenceHistoryInput
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Street address is required."), StringLength(100, ErrorMessage = "Street address must be 100 characters or fewer.")]
        public string StreetAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required."), StringLength(50, ErrorMessage = "City must be 50 characters or fewer.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required."), StringLength(50, ErrorMessage = "State must be 50 characters or fewer.")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "ZIP code is required."), StringLength(10, ErrorMessage = "ZIP code must be 10 characters or fewer.")]
        public string ZipCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Landlord first name is required."), StringLength(50, ErrorMessage = "Landlord first name must be 50 characters or fewer.")]
        public string LandlordFirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Landlord last name is required."), StringLength(50, ErrorMessage = "Landlord last name must be 50 characters or fewer.")]
        public string LandlordLastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Landlord phone number is required."), StringLength(50, ErrorMessage = "Landlord phone number must be 50 characters or fewer.")]
        public string LandlordPhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Move-in date is required."), DataType(DataType.Date)]
        public DateTime? MoveInDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? MoveOutDate { get; set; }
    }

    private void ValidateResidenceHistory()
    {
        if (Input.ResidenceHistory.Count == 0)
        {
            ModelState.AddModelError("Input.ResidenceHistory", "Add at least one residence.");
            return;
        }

        if (Input.ResidenceHistory.Count > 20)
        {
            ModelState.AddModelError("Input.ResidenceHistory", "You can provide at most 20 residences.");
        }

        var today = DateTime.UtcNow.Date;
        for (var index = 0; index < Input.ResidenceHistory.Count; index++)
        {
            var residence = Input.ResidenceHistory[index];
            var prefix = $"Input.ResidenceHistory[{index}]";
            var moveOutKey = $"{prefix}.MoveOutDate";

            if (string.IsNullOrWhiteSpace(residence.StreetAddress))
                ModelState.AddModelError($"{prefix}.StreetAddress", "Street address is required.");
            if (string.IsNullOrWhiteSpace(residence.City))
                ModelState.AddModelError($"{prefix}.City", "City is required.");
            if (string.IsNullOrWhiteSpace(residence.State))
                ModelState.AddModelError($"{prefix}.State", "State is required.");
            if (string.IsNullOrWhiteSpace(residence.ZipCode))
                ModelState.AddModelError($"{prefix}.ZipCode", "ZIP code is required.");
            if (string.IsNullOrWhiteSpace(residence.LandlordFirstName))
                ModelState.AddModelError($"{prefix}.LandlordFirstName", "Landlord first name is required.");
            if (string.IsNullOrWhiteSpace(residence.LandlordLastName))
                ModelState.AddModelError($"{prefix}.LandlordLastName", "Landlord last name is required.");
            if (string.IsNullOrWhiteSpace(residence.LandlordPhoneNumber))
                ModelState.AddModelError($"{prefix}.LandlordPhoneNumber", "Landlord phone is required.");
            if (residence.MoveInDate is null)
                ModelState.AddModelError($"{prefix}.MoveInDate", "Move-in date is required.");
            else if (residence.MoveInDate.Value.Date > today)
                ModelState.AddModelError($"{prefix}.MoveInDate", "Move-in date cannot be in the future.");
            if (residence.MoveOutDate.HasValue &&
                residence.MoveInDate.HasValue &&
                !ApplicationResidenceHistory.HasValidDateRange(
                    residence.MoveInDate.Value,
                    residence.MoveOutDate.Value))
            {
                ModelState.AddModelError($"{prefix}.MoveOutDate", "Move-out date cannot be before move-in date.");
            }
            if (index > 0 && residence.MoveOutDate is null)
                ModelState.AddModelError("Input.ResidenceHistory", "Move-out date is required except for the most recent residence.");
            var previousMoveInDate = index > 0
                ? Input.ResidenceHistory[index - 1].MoveInDate
                : null;
            if (index > 0 &&
                residence.MoveOutDate is { } moveOutDate &&
                previousMoveInDate is { } previousDate &&
                moveOutDate.Date > previousDate.Date)
            {
                ModelState.AddModelError($"{prefix}.MoveOutDate",
                    "Residence history must be ordered from most recent to oldest without overlapping dates.");
            }
            if (index == 0)
            {
                ModelState.Remove(moveOutKey);
                ModelState.Remove($"{moveOutKey}.Value");
            }
        }
    }

    private Task<Application?> FindExistingApplicationAsync(int unitId, Guid userId) =>
        _db.Applications
            .Include(item => item.ApplicationResidenceHistory)
            .Include(item => item.ApplicationStatus)
            .Include(item => item.StatusHistory)
            .Where(item => item.UnitId == unitId &&
                           item.UserId == userId &&
                           new[] { 1, 2, 3, 4 }.Contains(item.ApplicationStatusId))
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync();
}

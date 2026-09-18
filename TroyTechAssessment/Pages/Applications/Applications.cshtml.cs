using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Pages.Applications;

[Authorize]
public class ApplicationsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public ApplicationsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IList<Application> Applications { get; private set; } = [];
    public IList<SelectListItem> StatusOptions { get; private set; } = [];
    public IList<SelectListItem> PropertyOptions { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "id";
    [BindProperty(SupportsGet = true)] public string Direction { get; set; } = "desc";
    
    [BindProperty(SupportsGet = true)] 
    public bool MyClaims {get; set;} = false;
    
    [BindProperty(SupportsGet = true)]
    public int? StatusId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PropertyId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    public int TotalItems { get; private set; }
    public Guid? CurrentManagerId { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        var isManager = User.IsInRole("Manager");
        var isApplicant = User.IsInRole("Applicant");
        if (isManager == isApplicant)
        {
            return Forbid();
        }

        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var hasUserId = Guid.TryParse(userIdValue, out var userId);
        if (!hasUserId)
        {
            return Forbid();
        }
        CurrentManagerId = isManager ? userId : null;

        var visiblePropertyQuery = _db.Properties.AsNoTracking();

        if (isManager)
        {
            visiblePropertyQuery = visiblePropertyQuery.Where(property =>
                _db.ManagerProperties.Any(assignment =>
                    assignment.ManagerId == userId &&
                    assignment.PropertyId == property.Id));
        }
        else
        {
            visiblePropertyQuery = visiblePropertyQuery.Where(property =>
                _db.Applications.Any(application =>
                    application.UserId == userId &&
                    application.Unit.PropertyId == property.Id));
        }

        PropertyOptions = await visiblePropertyQuery
            .OrderBy(property => property.Name)
            .Select(property => new SelectListItem
            {
                Value = property.Id.ToString(),
                Text = property.Name,
                Selected = PropertyId == property.Id
            })
            .ToListAsync();
        PropertyOptions.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "All properties",
            Selected = !PropertyId.HasValue
        });

        StatusOptions = await _db.ApplicationStatuses
            .AsNoTracking()
            .OrderBy(status => status.Id)
            .Select(status => new SelectListItem
            {
                Value = status.Id.ToString(),
                Text = status.Status,
                Selected = StatusId.HasValue && status.Id == StatusId.Value
            })
            .ToListAsync();

        if (StatusId.HasValue)
        {
            StatusOptions.Insert(0, new SelectListItem { Value = "", Text = "All statuses", Selected = false });
        }
        else
        {
            StatusOptions.Insert(0, new SelectListItem { Value = "", Text = "All statuses", Selected = true });
        }

        var query = _db.Applications
            .Include(application => application.Unit)
            .ThenInclude(unit => unit.Property)
            .Include(application => application.ApplicationStatus)
            .Include(application => application.ClaimedByManager)
            .AsNoTracking();

        if (isApplicant)
        {
            query = query.Where(application => application.UserId == userId);
        }
        else
        {
            query = query.Where(application =>
                _db.ManagerProperties.Any(assignment =>
                    assignment.ManagerId == userId &&
                    assignment.PropertyId == application.Unit.PropertyId));
        }

        if (PropertyId.HasValue)
        {
            query = query.Where(application => application.Unit.PropertyId == PropertyId.Value);
        }

        if (StatusId.HasValue)
        {
            query = query.Where(application => application.ApplicationStatusId == StatusId.Value);
        }

        var normalizedSearch = SearchTerm?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var searchText = normalizedSearch;
            query = query.Where(application =>
                application.FirstName.Contains(searchText)
                || application.LastName.Contains(searchText)
                || application.Email.Contains(searchText)
                || application.PhoneNumber.Contains(searchText)
                || application.CurrentStreetAddress.Contains(searchText)
                || application.CurrentCity.Contains(searchText)
                || application.CurrentState.Contains(searchText)
                || application.CurrentZipCode.Contains(searchText)
                || application.Unit.Number.Contains(searchText)
                || application.Unit.Property.Name.Contains(searchText)
                || application.ApplicationStatus.Status.Contains(searchText));
        }

        if (MyClaims){
            query = query.Where(application => application.ClaimedByManagerId == userId);
        }

        PageNumber = Math.Max(1, PageNumber);
        PageSize = NormalizePageSize(PageSize);
        TotalItems = await query.CountAsync();
        query = Sort.ToLowerInvariant() switch
        {
            "applicant" => Direction == "desc"
                ? query.OrderByDescending(application => application.LastName).ThenByDescending(application => application.FirstName)
                : query.OrderBy(application => application.LastName).ThenBy(application => application.FirstName),
            "property" => Direction == "desc"
                ? query.OrderByDescending(application => application.Unit.Property.Name)
                : query.OrderBy(application => application.Unit.Property.Name),
            "status" => Direction == "desc"
                ? query.OrderByDescending(application => application.ApplicationStatus.Status)
                : query.OrderBy(application => application.ApplicationStatus.Status),
            _ => Direction == "asc"
                ? query.OrderBy(application => application.Id)
                : query.OrderByDescending(application => application.Id)
        };
        Applications = await query
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnGetJsonAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Challenge();

        var result = await OnGetAsync();
        if (result is not PageResult)
            return result;

        return new JsonResult(new
        {
            items = Applications.Select(application => new
            {
                id = application.Id,
                applicant = $"{application.FirstName} {application.LastName}",
                email = application.Email,
                phone = application.PhoneNumber,
                property = application.Unit.Property.Name,
                unit = application.Unit.Number,
                status = application.ApplicationStatus.Status
            }),
            page = PageNumber,
            pageSize = PageSize,
            totalItems = TotalItems
        });
    }

    public async Task<IActionResult> OnPostClaimAsync(int applicationId)
    {
        if (!TryGetCurrentManagerId(out var managerId))
            return Forbid();

        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        var application = await _db.Applications
            .Include(item => item.Unit)
            .SingleOrDefaultAsync(item =>
                item.Id == applicationId &&
                item.ApplicationStatusId == 2 &&
                item.ClaimedByManagerId == null &&
                _db.ManagerProperties.Any(assignment =>
                    assignment.ManagerId == managerId &&
                    assignment.PropertyId == item.Unit.PropertyId));
        if (application is null)
            return NotFound();

        application.ClaimedByManagerId = managerId;
        application.ClaimedAtUtc = DateTime.UtcNow;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return StatusCode(StatusCodes.Status409Conflict,
                "This application changed while it was being claimed. Refresh and try again.");
        }
        await transaction.CommitAsync();
        return RedirectToPage(new { PageNumber, PageSize, Sort, Direction, StatusId, PropertyId, SearchTerm });
    }

    public async Task<IActionResult> OnPostReleaseAsync(int applicationId)
    {
        if (!TryGetCurrentManagerId(out var managerId))
            return Forbid();

        var application = await _db.Applications
            .Include(item => item.Unit)
            .SingleOrDefaultAsync(item =>
                item.Id == applicationId &&
                item.ApplicationStatusId == 2 &&
                item.ClaimedByManagerId == managerId &&
                _db.ManagerProperties.Any(assignment =>
                    assignment.ManagerId == managerId &&
                    assignment.PropertyId == item.Unit.PropertyId));
        if (application is null)
            return NotFound();

        application.ClaimedByManagerId = null;
        application.ClaimedAtUtc = null;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return StatusCode(StatusCodes.Status409Conflict,
                "This application changed while it was being released. Refresh and try again.");
        }
        return RedirectToPage(new { PageNumber, PageSize, Sort, Direction, StatusId, PropertyId, SearchTerm });
    }

    private bool TryGetCurrentManagerId(out Guid managerId) =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out managerId)
        && User.IsInRole("Manager");

    private static int NormalizePageSize(int value) => value is 10 or 25 or 50 ? value : 10;
}

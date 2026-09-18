using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;
using TroyTechAssessment.Services;
using TroyTechAssessment.Pages.Units;

namespace TroyTechAssessment.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UnitService _unitService;

    public IndexModel(
        ApplicationDbContext db,
        UnitService unitService)
    {
        _db = db;
        _unitService = unitService;
    }

    [BindProperty]
    public ViewModel Input { get; set; } = new();

    public IReadOnlyList<Unit> Units { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "property";
    [BindProperty(SupportsGet = true)] public string Direction { get; set; } = "asc";
    [BindProperty(Name = "propertyId", SupportsGet = true)] public int? UnitPropertyId { get; set; }
    public int TotalUnits { get; private set; }
    public UnitsPageViewModel UnitsTable => new()
    {
        Units = Units,
        Page = PageNumber,
        PageSize = PageSize,
        TotalItems = TotalUnits,
        Sort = Sort,
        Direction = Direction,
        PropertyId = UnitPropertyId,
        PropertyOptions = Properties.ToList(),
        CanManageUnits = User.IsInRole("Manager"),
        CanApply = User.IsInRole("Applicant")
    };
    public IList<SelectListItem> UnitTypes { get; private set; } = [];
    public IList<Property> Properties { get; private set; } = [];
    public string Section { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string? section = null, int? unitId = null, int? propertyId = null)
    {
        Section = section ?? string.Empty;
        await LoadPageDataAsync();
        await LoadUnitsPageAsync();

        if (Section == "editproperty")
        {
            if (!User.IsInRole("Manager"))
            {
                return Forbid();
            }

            var property = propertyId is null
                ? null
                : await _db.Properties.AsNoTracking().SingleOrDefaultAsync(item =>
                    item.Id == propertyId &&
                    _db.ManagerProperties.Any(assignment =>
                        assignment.ManagerId == CurrentUserId &&
                        assignment.PropertyId == item.Id));
            if (property is null)
            {
                return NotFound();
            }

            Input.PropertyId = property.Id;
            Input.PropertyName = property.Name;
            Input.StreetAddress = property.StreetAddress;
            Input.City = property.City;
            Input.State = property.State;
            Input.ZipCode = property.ZipCode;
        }
        else if (Section == "editunit")
        {
            if (!User.IsInRole("Manager"))
            {
                return Forbid();
            }

            var unit = unitId is null
                ? null
                : await _unitService.GetManagedUnitAsync(User, unitId.Value);
            if (unit is null)
            {
                return NotFound();
            }

            Input.UnitId = unit.Id;
            Input.Number = unit.Number;
            Input.Bedrooms = unit.Bedrooms;
            Input.MonthlyRent = unit.MonthlyRent;
            Input.PropertyId = unit.PropertyId;
            Input.UnitTypeId = unit.UnitTypeId;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Section = Input.Action switch
        {
            "AddProperty" => "property",
            "EditProperty" => "editproperty",
            "DeleteProperty" => string.Empty,
            "AddUnit" => "unit",
            "EditUnit" => "editunit",
            "DeleteUnit" => string.Empty,
            _ => string.Empty
        };

        await LoadPageDataAsync();
        await LoadUnitsPageAsync();

        return Input.Action switch
        {
            "AddProperty" => await AddPropertyAsync(),
            "EditProperty" => await EditPropertyAsync(),
            "DeleteProperty" => await DeletePropertyAsync(),
            "AddUnit" => await AddUnitAsync(),
            "EditUnit" => await EditUnitAsync(),
            "DeleteUnit" => await DeleteUnitAsync(),
            _ => Page()
        };
    }

    public async Task<IActionResult> OnGetUnitsJsonAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Challenge();
        if (!User.IsInRole("Applicant") && !User.IsInRole("Manager"))
            return Forbid();

        await LoadUnitsPageAsync();
        return new JsonResult(new
        {
            items = Units.Select(unit => new
            {
                id = unit.Id, property = unit.Property.Name, unit = unit.Number,
                bedrooms = unit.Bedrooms, monthlyRent = unit.MonthlyRent,
                type = unit.UnitType.UnitTypeName
            }),
            page = PageNumber, pageSize = PageSize, totalItems = TotalUnits
        });
    }

    private async Task LoadUnitsPageAsync()
    {
        PageNumber = Math.Max(1, PageNumber);
        PageSize = NormalizePageSize(PageSize);
        var query = _db.Units
            .Include(unit => unit.Property)
            .Include(unit => unit.UnitType)
            .AsNoTracking();
        if (UnitPropertyId.HasValue)
        {
            query = query.Where(unit => unit.PropertyId == UnitPropertyId.Value);
        }
        if (User.IsInRole("Applicant"))
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(unit => !unit.Leases.Any(lease =>
                lease.StartDate <= today && lease.EndDate >= today));
        }
        else if (User.IsInRole("Manager"))
        {
            query = query.Where(unit => _db.ManagerProperties.Any(assignment =>
                assignment.ManagerId == CurrentUserId &&
                assignment.PropertyId == unit.PropertyId));
        }

        TotalUnits = await query.CountAsync();
        query = Sort.ToLowerInvariant() switch
        {
            "unit" => Direction == "desc" ? query.OrderByDescending(unit => unit.Number) : query.OrderBy(unit => unit.Number),
            "rent" => Direction == "desc" ? query.OrderByDescending(unit => unit.MonthlyRent) : query.OrderBy(unit => unit.MonthlyRent),
            "bedrooms" => Direction == "desc" ? query.OrderByDescending(unit => unit.Bedrooms) : query.OrderBy(unit => unit.Bedrooms),
            _ => Direction == "desc" ? query.OrderByDescending(unit => unit.Property.Name) : query.OrderBy(unit => unit.Property.Name)
        };
        Units = await query.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync();
    }

    private static int NormalizePageSize(int value) => value is 10 or 25 or 50 ? value : 10;

    private async Task<IActionResult> AddPropertyAsync()
    {
        if (!User.IsInRole("Manager"))
        {
            return Forbid();
        }

        if (!ValidatePropertyInput())
        {
            return Page();
        }

        var property = new Property
        {
            Name = Input.PropertyName,
            StreetAddress = Input.StreetAddress,
            City = Input.City,
            State = Input.State,
            ZipCode = Input.ZipCode
        };
        _db.Properties.Add(property);
        await _db.SaveChangesAsync();
        _db.ManagerProperties.Add(new ManagerProperty
        {
            ManagerId = CurrentUserId,
            PropertyId = property.Id
        });
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task<IActionResult> EditPropertyAsync()
    {
        if (!User.IsInRole("Manager"))
        {
            return Forbid();
        }

        if (Input.PropertyId <= 0 || !ValidatePropertyInput())
        {
            return Page();
        }

        var property = await _db.Properties.SingleOrDefaultAsync(item =>
            item.Id == Input.PropertyId &&
            _db.ManagerProperties.Any(assignment =>
                assignment.ManagerId == CurrentUserId &&
                assignment.PropertyId == item.Id));
        if (property is null)
        {
            return NotFound();
        }

        property.Name = Input.PropertyName;
        property.StreetAddress = Input.StreetAddress;
        property.City = Input.City;
        property.State = Input.State;
        property.ZipCode = Input.ZipCode;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task<IActionResult> DeletePropertyAsync()
    {
        if (!User.IsInRole("Manager"))
        {
            return Forbid();
        }

        var property = await _db.Properties.SingleOrDefaultAsync(item =>
            item.Id == Input.PropertyId &&
            _db.ManagerProperties.Any(assignment =>
                assignment.ManagerId == CurrentUserId &&
                assignment.PropertyId == item.Id));
        if (property is null)
        {
            return NotFound();
        }

        var hasUnits = await _db.Units.AsNoTracking().AnyAsync(unit => unit.PropertyId == property.Id);
        if (hasUnits)
        {
            ModelState.AddModelError(string.Empty, "This property cannot be deleted while it has units.");
            return Page();
        }

        _db.Properties.Remove(property);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private bool ValidatePropertyInput()
    {
        if (string.IsNullOrWhiteSpace(Input.PropertyName) || Input.PropertyName.Length > 200 ||
            string.IsNullOrWhiteSpace(Input.StreetAddress) || Input.StreetAddress.Length > 200 ||
            string.IsNullOrWhiteSpace(Input.City) || Input.City.Length > 100 ||
            string.IsNullOrWhiteSpace(Input.State) || Input.State.Length > 100 ||
            string.IsNullOrWhiteSpace(Input.ZipCode) || Input.ZipCode.Length > 20)
        {
            AddPropertyValidationErrors();
            return false;
        }

        return true;
    }

    private void AddPropertyValidationErrors()
    {
        AddTextValidationError(nameof(Input.PropertyName), Input.PropertyName, "Property name", 200);
        AddTextValidationError(nameof(Input.StreetAddress), Input.StreetAddress, "Street address", 200);
        AddTextValidationError(nameof(Input.City), Input.City, "City", 100);
        AddTextValidationError(nameof(Input.State), Input.State, "State", 100);
        AddTextValidationError(nameof(Input.ZipCode), Input.ZipCode, "ZIP code", 20);
    }

    private void AddTextValidationError(string key, string value, string label, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            ModelState.AddModelError(key, $"{label} is required.");
        else if (value.Length > maxLength)
            ModelState.AddModelError(key, $"{label} must be {maxLength} characters or fewer.");
    }

    private async Task<IActionResult> AddUnitAsync()
    {
        if (!User.IsInRole("Manager"))
        {
            return Forbid();
        }

        if (!ValidateUnitInput())
        {
            return Page();
        }

        if (!await _db.Properties.AsNoTracking().AnyAsync(property =>
                property.Id == Input.PropertyId &&
                _db.ManagerProperties.Any(assignment =>
                    assignment.ManagerId == CurrentUserId &&
                    assignment.PropertyId == property.Id)))
        {
            ModelState.AddModelError(nameof(Input.PropertyId), "Select a managed property.");
            return Page();
        }

        if (!await _db.UnitTypes.AsNoTracking().AnyAsync(type =>
                type.Id == Input.UnitTypeId && type.Active))
        {
            ModelState.AddModelError(nameof(Input.UnitTypeId), "Select an active unit type.");
            return Page();
        }

        _db.Units.Add(new Unit
        {
            Number = Input.Number.Trim(),
            Bedrooms = Input.Bedrooms,
            MonthlyRent = Input.MonthlyRent,
            PropertyId = Input.PropertyId,
            UnitTypeId = Input.UnitTypeId
        });
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task<IActionResult> EditUnitAsync()
    {
        if (!User.IsInRole("Manager"))
        {
            return Forbid();
        }

        if (Input.UnitId <= 0 || !ValidateUnitInput())
        {
            return Page();
        }

        var unit = await _db.Units.SingleOrDefaultAsync(item =>
            item.Id == Input.UnitId &&
            _db.ManagerProperties.Any(assignment =>
                assignment.ManagerId == CurrentUserId &&
                assignment.PropertyId == item.PropertyId));
        if (unit is null)
        {
            return NotFound();
        }

        if (!await _db.Properties.AsNoTracking().AnyAsync(property =>
                property.Id == Input.PropertyId &&
                _db.ManagerProperties.Any(assignment =>
                    assignment.ManagerId == CurrentUserId &&
                    assignment.PropertyId == property.Id)))
        {
            ModelState.AddModelError(nameof(Input.PropertyId), "Select a managed property.");
            return Page();
        }

        if (!await _db.UnitTypes.AsNoTracking().AnyAsync(type =>
                type.Id == Input.UnitTypeId && (type.Active || type.Id == unit.UnitTypeId)))
        {
            ModelState.AddModelError(nameof(Input.UnitTypeId), "Select an active unit type.");
            return Page();
        }

        unit.Number = Input.Number.Trim();
        unit.Bedrooms = Input.Bedrooms;
        unit.MonthlyRent = Input.MonthlyRent;
        unit.PropertyId = Input.PropertyId;
        unit.UnitTypeId = Input.UnitTypeId;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task<IActionResult> DeleteUnitAsync()
    {
        if (!User.IsInRole("Manager"))
        {
            return Forbid();
        }

        if (Input.UnitId <= 0)
        {
            return BadRequest();
        }

        var unit = await _db.Units.SingleOrDefaultAsync(item =>
            item.Id == Input.UnitId &&
            _db.ManagerProperties.Any(assignment =>
                assignment.ManagerId == CurrentUserId &&
                assignment.PropertyId == item.PropertyId));
        if (unit is null)
        {
            return NotFound();
        }

        if (await _db.Applications.AsNoTracking().AnyAsync(application => application.UnitId == unit.Id) ||
            await _db.Leases.AsNoTracking().AnyAsync(lease => lease.UnitId == unit.Id))
        {
            ModelState.AddModelError(string.Empty, "This unit cannot be deleted because it has applications or lease history.");
            return Page();
        }

        _db.Units.Remove(unit);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadPageDataAsync()
    {
        Units = await _unitService.GetVisibleUnitsAsync(User);

        UnitTypes = await _db.UnitTypes
            .AsNoTracking()
            .Where(type => type.Active)
            .OrderBy(type => type.UnitTypeName)
            .Select(type => new SelectListItem
            {
                Value = type.Id.ToString(),
                Text = type.UnitTypeName
            })
            .ToListAsync();

        Properties = await _db.Properties
            .AsNoTracking()
            .Where(property => !User.IsInRole("Manager") ||
                _db.ManagerProperties.Any(assignment =>
                    assignment.ManagerId == CurrentUserId &&
                    assignment.PropertyId == property.Id))
            .OrderBy(property => property.Name)
            .ToListAsync();
    }

    private bool ValidateUnitInput()
    {
        if (string.IsNullOrWhiteSpace(Input.Number) || Input.Number.Trim().Length > 10)
        {
            ModelState.AddModelError(nameof(Input.Number), "Enter a unit number of 10 characters or fewer.");
            return false;
        }

        if (!Unit.BedroomOptions.Contains(Input.Bedrooms))
        {
            ModelState.AddModelError(nameof(Input.Bedrooms), "Select a valid bedroom count.");
            return false;
        }

        if (Input.MonthlyRent <= 0)
        {
            ModelState.AddModelError(nameof(Input.MonthlyRent), "Monthly rent must be greater than zero.");
            return false;
        }

        if (Input.PropertyId <= 0)
        {
            ModelState.AddModelError(nameof(Input.PropertyId), "Select a property.");
            return false;
        }

        if (Input.UnitTypeId <= 0)
        {
            ModelState.AddModelError(nameof(Input.UnitTypeId), "Select a unit type.");
            return false;
        }

        return true;
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)
            ? userId
            : Guid.Empty;

    public class ViewModel
    {
        public string Action { get; set; } = string.Empty;
        [Display(Name = "Unit ID")]
        public int UnitId { get; set; }
        [Display(Name = "Unit number")]
        public string Number { get; set; } = string.Empty;
        public int Bedrooms { get; set; }
        [Display(Name = "Monthly rent")]
        public double MonthlyRent { get; set; }
        [Display(Name = "Property")]
        public int PropertyId { get; set; }
        [Display(Name = "Unit type")]
        public int UnitTypeId { get; set; }
        [Display(Name = "Property name")]
        public string PropertyName { get; set; } = string.Empty;
        [Display(Name = "Street address")]
        public string StreetAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        [Display(Name = "ZIP code")]
        public string ZipCode { get; set; } = string.Empty;
    }
}

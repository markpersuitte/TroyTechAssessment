using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Pages.Properties;

[Authorize(Roles = "Manager")]
public class PropertiesModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public PropertiesModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IList<Property> Properties { get; private set; } = [];

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string Section { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string? section = null, int? propertyId = null)
    {
        Section = section ?? string.Empty;
        await LoadPropertiesAsync();

        if (Section == "editproperty")
        {
            var property = propertyId is null
                ? null
                : await _db.Properties.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == propertyId &&
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

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Section = Input.Action switch
        {
            "AddProperty" => "property",
            "EditProperty" => "editproperty",
            _ => string.Empty
        };

        await LoadPropertiesAsync();

        return Input.Action switch
        {
            "AddProperty" => await AddPropertyAsync(),
            "EditProperty" => await EditPropertyAsync(),
            "DeleteProperty" => await DeletePropertyAsync(),
            _ => Page()
        };
    }

    private async Task<IActionResult> AddPropertyAsync()
    {
        if (!ValidatePropertyInput())
        {
            return Page();
        }

        var property = CreateProperty();
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
        if (Input.PropertyId <= 0)
        {
            return BadRequest();
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

        if (await _db.Units.AsNoTracking().AnyAsync(unit => unit.PropertyId == property.Id))
        {
            ModelState.AddModelError(string.Empty, "This property cannot be deleted while it has units.");
            return Page();
        }

        _db.Properties.Remove(property);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadPropertiesAsync()
    {
        Properties = await _db.Properties
            .AsNoTracking()
            .Include(property => property.Units)
            .Where(property => _db.ManagerProperties.Any(assignment =>
                assignment.ManagerId == CurrentUserId &&
                assignment.PropertyId == property.Id))
            .OrderBy(property => property.Name)
            .ToListAsync();
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)
            ? userId
            : Guid.Empty;



    private bool ValidatePropertyInput()
    {
        AddTextValidationError(nameof(Input.PropertyName), Input.PropertyName, "Property name", 200);
        AddTextValidationError(nameof(Input.StreetAddress), Input.StreetAddress, "Street address", 200);
        AddTextValidationError(nameof(Input.City), Input.City, "City", 100);
        AddTextValidationError(nameof(Input.State), Input.State, "State", 100);
        AddTextValidationError(nameof(Input.ZipCode), Input.ZipCode, "ZIP code", 20);
        if (!ModelState.IsValid)
        {
            return false;
        }

        return true;
    }

    private void AddTextValidationError(string key, string value, string label, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            ModelState.AddModelError(key, $"{label} is required.");
        else if (value.Length > maxLength)
            ModelState.AddModelError(key, $"{label} must be {maxLength} characters or fewer.");
    }

    private Property CreateProperty() => new()
    {
        Name = Input.PropertyName,
        StreetAddress = Input.StreetAddress,
        City = Input.City,
        State = Input.State,
        ZipCode = Input.ZipCode
    };

    public class InputModel
    {
        public string Action { get; set; } = string.Empty;
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
    }
}

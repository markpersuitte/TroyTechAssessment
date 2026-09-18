using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Controllers;

[Authorize(Roles = "Manager")]
public class PropertiesController(ApplicationDbContext db) : Controller{
    public async Task<IActionResult> Index()
    {
        var properties = await db.Properties
            .AsNoTracking()
            .Include(property => property.Units)
            .Include(property => property.ManagerProperties.Where(m => m.ManagerId == CurrentUserId))
            .OrderBy(property => property.Name)
            .ToListAsync();

        return View(properties);
    }

    [HttpGet]
    public async Task<IActionResult> Modal(int? id)
    {
        if (id is null)
            return PartialView("_PropertyModal", new PropertyInput());

        var property = await ManagedPropertyQuery()
            .SingleOrDefaultAsync(item => item.Id == id);
        return property is null
            ? NotFound()
            : PartialView("_PropertyModal", PropertyInput.From(property));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PropertyInput input)
    {
        Validate(input);
        if (!ModelState.IsValid)
            return BadRequest(PartialView("_PropertyModal", input));

        Property property;
        if (input.Id is null)
        {
            property = new Property();
            db.Properties.Add(property);
        }
        else
        {
            property = await ManagedPropertyQuery()
                .SingleOrDefaultAsync(item => item.Id == input.Id.Value)
                ?? throw new KeyNotFoundException("Property was not found.");
        }

        property.Name = input.Name.Trim();
        property.StreetAddress = input.StreetAddress.Trim();
        property.City = input.City.Trim();
        property.State = input.State.Trim();
        property.ZipCode = input.ZipCode.Trim();
        await db.SaveChangesAsync();
        
        return Json(new { success = true, url = Url.Action(nameof(Index)) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var property = await ManagedPropertyQuery().SingleOrDefaultAsync(item => item.Id == id);
        if (property is null) return NotFound();
        if (await db.Units.AnyAsync(unit => unit.PropertyId == id))
            return BadRequest("This property cannot be deleted while it has units.");

        var assignments = await db.ManagerProperties
            .Where(assignment => assignment.PropertyId == id)
            .ToListAsync();
        db.ManagerProperties.RemoveRange(assignments);
        db.Properties.Remove(property);
        await db.SaveChangesAsync();
        return Json(new { success = true, url = Url.Action(nameof(Index)) });
    }

    private IQueryable<Property> ManagedPropertyQuery() =>
        db.Properties.Where(property => db.ManagerProperties.Any(assignment =>
            assignment.ManagerId == CurrentUserId && assignment.PropertyId == property.Id));

    private void Validate(PropertyInput input)
    {
        AddTextValidationError(nameof(input.Name), input.Name, "Property name", 200);
        AddTextValidationError(nameof(input.StreetAddress), input.StreetAddress, "Street address", 200);
        AddTextValidationError(nameof(input.City), input.City, "City", 100);
        AddTextValidationError(nameof(input.State), input.State, "State", 100);
        AddTextValidationError(nameof(input.ZipCode), input.ZipCode, "ZIP code", 20);
    }

    private void AddTextValidationError(string key, string value, string label, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            ModelState.AddModelError(key, $"{label} is required.");
        else if (value.Length > maxLength)
            ModelState.AddModelError(key, $"{label} must be {maxLength} characters or fewer.");
    }

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    public sealed class PropertyInput
    {
        public int? Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string StreetAddress { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string ZipCode { get; init; } = string.Empty;

        public static PropertyInput From(Property property) => new()
        {
            Id = property.Id, Name = property.Name, StreetAddress = property.StreetAddress,
            City = property.City, State = property.State, ZipCode = property.ZipCode
        };
    }
}

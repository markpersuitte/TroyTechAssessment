using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Controllers;

[Authorize(Roles = "Manager")]
public class UnitsController : Controller
{
    private readonly ApplicationDbContext _db;
    public UnitsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Modal(int? id)
    {
        var properties = await ManagedProperties().OrderBy(property => property.Name).ToListAsync();
        var unitTypes = await _db.UnitTypes.AsNoTracking().Where(type => type.Active)
            .OrderBy(type => type.UnitTypeName).ToListAsync();
        var input = new UnitInput { PropertyOptions = properties, UnitTypeOptions = unitTypes };

        if (id is not null)
        {
            var unit = await ManagedUnits().SingleOrDefaultAsync(item => item.Id == id.Value);
            if (unit is null) return NotFound();
            input.Id = unit.Id;
            input.Number = unit.Number;
            input.Bedrooms = unit.Bedrooms;
            input.MonthlyRent = unit.MonthlyRent;
            input.PropertyId = unit.PropertyId;
            input.UnitTypeId = unit.UnitTypeId;
            if (unit.UnitType is { Active: false } && !input.UnitTypeOptions.Any(type => type.Id == unit.UnitTypeId))
                input.UnitTypeOptions.Add(unit.UnitType);
        }

        return PartialView("_UnitModal", input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UnitInput input)
    {
        await LoadOptionsAsync(input);
        if (string.IsNullOrWhiteSpace(input.Number) || input.Number.Length > 10)
            ModelState.AddModelError(nameof(input.Number), "Enter a unit number of 10 characters or fewer.");
        if (!Unit.BedroomOptions.Contains(input.Bedrooms))
            ModelState.AddModelError(nameof(input.Bedrooms), "Select a valid bedroom count.");
        if (input.MonthlyRent <= 0)
            ModelState.AddModelError(nameof(input.MonthlyRent), "Monthly rent must be greater than zero.");
        if (!await ManagedProperties().AnyAsync(property => property.Id == input.PropertyId))
            ModelState.AddModelError(nameof(input.PropertyId), "Select a managed property.");

        var existing = input.Id is null ? null : await ManagedUnits().Include(item => item.UnitType)
            .SingleOrDefaultAsync(item => item.Id == input.Id.Value);
        if (input.Id is not null && existing is null) return NotFound();
        if (!await _db.UnitTypes.AnyAsync(type => type.Id == input.UnitTypeId &&
                (type.Active || (existing != null && type.Id == existing.UnitTypeId))))
            ModelState.AddModelError(nameof(input.UnitTypeId), "Select an active unit type.");
        if (!ModelState.IsValid) return BadRequest(PartialView("_UnitModal", input));

        var unit = existing ?? new Unit();
        unit.Number = input.Number.Trim();
        unit.Bedrooms = input.Bedrooms;
        unit.MonthlyRent = input.MonthlyRent;
        unit.PropertyId = input.PropertyId;
        unit.UnitTypeId = input.UnitTypeId;
        if (existing is null) _db.Units.Add(unit);
        await _db.SaveChangesAsync();
        return Json(new { success = true, url = Url.Page("/Index") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var unit = await ManagedUnits().SingleOrDefaultAsync(item => item.Id == id);
        if (unit is null) return NotFound();
        if (await _db.Applications.AnyAsync(application => application.UnitId == id))
            return BadRequest("This unit cannot be deleted because it has applications.");
        if (await _db.Leases.AnyAsync(lease => lease.UnitId == id))
            return BadRequest("This unit cannot be deleted because it has leases.");
        _db.Units.Remove(unit);
        await _db.SaveChangesAsync();
        return Json(new { success = true, url = Url.Page("/Index") });
    }

    private IQueryable<Property> ManagedProperties() => _db.Properties.Where(property =>
        _db.ManagerProperties.Any(assignment => assignment.ManagerId == CurrentUserId &&
                                                assignment.PropertyId == property.Id));
    private IQueryable<Unit> ManagedUnits() => _db.Units.Where(unit =>
        _db.ManagerProperties.Any(assignment => assignment.ManagerId == CurrentUserId &&
                                                assignment.PropertyId == unit.PropertyId));
    private async Task LoadOptionsAsync(UnitInput input)
    {
        input.PropertyOptions = await ManagedProperties().OrderBy(property => property.Name).ToListAsync();
        input.UnitTypeOptions = await _db.UnitTypes.AsNoTracking().Where(type => type.Active)
            .OrderBy(type => type.UnitTypeName).ToListAsync();
        if (input.Id is not null)
        {
            var currentTypeId = await ManagedUnits().Where(unit => unit.Id == input.Id)
                .Select(unit => unit.UnitTypeId).SingleOrDefaultAsync();
            var currentType = await _db.UnitTypes.FindAsync(currentTypeId);
            if (currentType is { Active: false } && !input.UnitTypeOptions.Any(type => type.Id == currentType.Id))
                input.UnitTypeOptions.Add(currentType);
        }
    }
    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    public sealed class UnitInput
    {
        public int? Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public int Bedrooms { get; set; }
        public double MonthlyRent { get; set; }
        public int PropertyId { get; set; }
        public int UnitTypeId { get; set; }
        public List<Property> PropertyOptions { get; set; } = [];
        public List<UnitType> UnitTypeOptions { get; set; } = [];
    }
}

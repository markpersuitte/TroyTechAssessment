using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Controllers;

[Authorize(Roles = "Manager")]
public class UnitTypesController : Controller
{
    private readonly ApplicationDbContext _db;
    public UnitTypesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.UnitTypes.AsNoTracking().OrderBy(type => type.UnitTypeName).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Modal(int? id)
    {
        if (id is null) return PartialView("_UnitTypeModal", new UnitTypeInput());
        var type = await _db.UnitTypes.FindAsync(id.Value);
        return type is null ? NotFound() : PartialView("_UnitTypeModal", new UnitTypeInput
        {
            Id = type.Id, Name = type.UnitTypeName, Active = type.Active
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UnitTypeInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 50)
            ModelState.AddModelError(nameof(input.Name), "Enter a unit type name of 50 characters or fewer.");
        if (!ModelState.IsValid)
            return BadRequest(PartialView("_UnitTypeModal", input));

        var type = input.Id is null ? new UnitType() : await _db.UnitTypes.FindAsync(input.Id.Value);
        if (type is null) return NotFound();
        type.UnitTypeName = input.Name.Trim();
        type.Active = input.Active;
        if (input.Id is null) _db.UnitTypes.Add(type);
        await _db.SaveChangesAsync();
        return Json(new { success = true, url = Url.Action(nameof(Index)) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var type = await _db.UnitTypes.FindAsync(id);
        if (type is null) return NotFound();
        if (await _db.Units.AnyAsync(unit => unit.UnitTypeId == id))
            return BadRequest("This unit type cannot be deleted while units use it.");
        _db.UnitTypes.Remove(type);
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    public sealed class UnitTypeInput
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
    }
}

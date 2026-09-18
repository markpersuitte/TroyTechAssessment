using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Pages.UnitTypes;

[Authorize(Roles = "Manager")]
public class UnitTypesModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public UnitTypesModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IList<UnitType> UnitTypes { get; private set; } = [];

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string Section { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string? section = null, int? unitTypeId = null)
    {
        Section = section ?? string.Empty;
        await LoadUnitTypesAsync();

        if (Section == "editunittype")
        {
            var unitType = unitTypeId is null
                ? null
                : await _db.UnitTypes.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == unitTypeId);

            if (unitType is null)
            {
                return NotFound();
            }

            Input.UnitTypeId = unitType.Id;
            Input.UnitTypeName = unitType.UnitTypeName;
            Input.Active = unitType.Active;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Section = Input.Action switch
        {
            "AddUnitType" => "unittype",
            "EditUnitType" => "editunittype",
            _ => string.Empty
        };

        await LoadUnitTypesAsync();

        return Input.Action switch
        {
            "AddUnitType" => await AddUnitTypeAsync(),
            "EditUnitType" => await EditUnitTypeAsync(),
            "DeleteUnitType" => await DeleteUnitTypeAsync(),
            _ => Page()
        };
    }

    private async Task<IActionResult> AddUnitTypeAsync()
    {
        if (!ValidateInput())
        {
            return Page();
        }

        _db.UnitTypes.Add(new UnitType
        {
            UnitTypeName = Input.UnitTypeName.Trim(),
            Active = Input.Active
        });
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task<IActionResult> EditUnitTypeAsync()
    {
        if (Input.UnitTypeId <= 0 || !ValidateInput())
        {
            return Page();
        }

        var unitType = await _db.UnitTypes
            .SingleOrDefaultAsync(item => item.Id == Input.UnitTypeId);
        if (unitType is null)
        {
            return NotFound();
        }

        unitType.UnitTypeName = Input.UnitTypeName.Trim();
        unitType.Active = Input.Active;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task<IActionResult> DeleteUnitTypeAsync()
    {
        if (Input.UnitTypeId <= 0)
        {
            return BadRequest();
        }

        var unitType = await _db.UnitTypes
            .SingleOrDefaultAsync(item => item.Id == Input.UnitTypeId);
        if (unitType is null)
        {
            return NotFound();
        }

        if (await _db.Units.AsNoTracking().AnyAsync(unit => unit.UnitTypeId == unitType.Id))
        {
            ModelState.AddModelError(string.Empty, "This unit type cannot be deleted while units use it.");
            return Page();
        }

        _db.UnitTypes.Remove(unitType);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadUnitTypesAsync()
    {
        UnitTypes = await _db.UnitTypes
            .AsNoTracking()
            .OrderBy(unitType => unitType.Id)
            .ToListAsync();
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Input.UnitTypeName) ||
            Input.UnitTypeName.Trim().Length > 50)
        {
            ModelState.AddModelError(string.Empty, "Enter a unit type name of 50 characters or fewer.");
            return false;
        }

        return true;
    }

    public class InputModel
    {
        public string Action { get; set; } = string.Empty;
        public int UnitTypeId { get; set; }
        public string UnitTypeName { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
    }
}

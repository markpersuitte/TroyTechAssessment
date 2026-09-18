using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.ViewComponents;

public class UnitRowViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(Unit unit)
    {
        var hasApplications = await db.Applications.AnyAsync(application => application.UnitId == unit.Id);
        var hasLeases = await db.Leases.AnyAsync(lease => lease.UnitId == unit.Id);
        return View(new UnitRowModel(unit, hasApplications, hasLeases));
    }

    public sealed record UnitRowModel(Unit Unit, bool HasApplications, bool HasLeases);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Pages.Leases;

[Authorize(Roles = "Manager")]
public class LeasesModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public LeasesModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IList<Lease> Leases { get; private set; } = [];
    
    public string Section { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string? section = null, int? leaseId = null)
    {
        Section = section ?? string.Empty;
        await LoadLeasesAsync();
        return Page();
    }

    private async Task LoadLeasesAsync()
    {
        Leases = await _db.Leases
            .AsNoTracking()
            .Include(lease => lease.Unit)
                .ThenInclude(unit => unit.Property)
            .Include(lease => lease.Unit)
                .ThenInclude(unit => unit.UnitType)
            .Include(lease => lease.Application)
            .Where(lease => _db.ManagerProperties.Any(assignment =>
                assignment.ManagerId == CurrentUserId &&
                assignment.PropertyId == lease.Application.Unit.PropertyId))
            .OrderByDescending(lease =>  lease.StartDate)
            .ToListAsync();
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)
            ? userId
            : Guid.Empty;
}

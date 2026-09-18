using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Services;

public sealed class UnitService
{
    private readonly ApplicationDbContext _db;

    public UnitService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Unit>> GetVisibleUnitsAsync(ClaimsPrincipal user)
    {
        var query = _db.Units
            .Include(unit => unit.Property)
            .Include(unit => unit.UnitType)
            .AsNoTracking();

        if (user.IsInRole("Applicant"))
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(unit => !unit.Leases.Any(lease =>
                lease.StartDate <= today && lease.EndDate >= today));
        }
        else if (user.IsInRole("Manager"))
        {
            var managerId = GetUserId(user);
            query = query.Where(unit => _db.ManagerProperties.Any(assignment =>
                assignment.ManagerId == managerId &&
                assignment.PropertyId == unit.PropertyId));
        }

        return await query.OrderBy(unit => unit.Number).ToListAsync();
    }

    public IQueryable<Property> GetManagedProperties(ClaimsPrincipal user) =>
        _db.Properties.Where(property =>
            _db.ManagerProperties.Any(assignment =>
                assignment.ManagerId == GetUserId(user) &&
                assignment.PropertyId == property.Id));

    public IQueryable<Unit> GetManagedUnits(ClaimsPrincipal user) =>
        _db.Units.Where(unit =>
            _db.ManagerProperties.Any(assignment =>
                assignment.ManagerId == GetUserId(user) &&
                assignment.PropertyId == unit.PropertyId));

    public async Task<List<Property>> GetManagedPropertiesListAsync(ClaimsPrincipal user) =>
        await GetManagedProperties(user).OrderBy(property => property.Name).ToListAsync();

    public async Task<List<UnitType>> GetAssignableUnitTypesAsync() =>
        await _db.UnitTypes.AsNoTracking()
            .Where(type => type.Active)
            .OrderBy(type => type.UnitTypeName)
            .ToListAsync();

    public async Task<Unit?> GetManagedUnitAsync(ClaimsPrincipal user, int id) =>
        await GetManagedUnits(user)
            .Include(unit => unit.UnitType)
            .SingleOrDefaultAsync(unit => unit.Id == id);

    public async Task<UnitType?> GetUnitTypeAsync(int id) =>
        await _db.UnitTypes.AsNoTracking().SingleOrDefaultAsync(type => type.Id == id);

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : Guid.Empty;
}

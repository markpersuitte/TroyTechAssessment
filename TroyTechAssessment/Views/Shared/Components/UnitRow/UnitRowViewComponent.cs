using System.Security.Claims;
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
        var applicantApplication = User.IsInRole("Applicant") &&
                                   User is ClaimsPrincipal claimsPrincipal &&
                                   Guid.TryParse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? await db.Applications
                .AsNoTracking()
                .Where(application =>
                    application.UnitId == unit.Id &&
                    application.Applicants.Any(applicant => applicant.UserId == userId) &&
                    new[] { 1, 2, 3 }.Contains(application.ApplicationStatusId))
                .OrderByDescending(application => application.Id)
                .Select(application => new ApplicantApplication(
                    application.Id,
                    application.ApplicationStatusId))
                .FirstOrDefaultAsync()
            : null;

        return View(new UnitRowModel(unit, hasApplications, hasLeases, applicantApplication));
    }

    public sealed record UnitRowModel(
        Unit Unit,
        bool HasApplications,
        bool HasLeases,
        ApplicantApplication? ApplicantApplication);

    public sealed record ApplicantApplication(int Id, int StatusId);
}

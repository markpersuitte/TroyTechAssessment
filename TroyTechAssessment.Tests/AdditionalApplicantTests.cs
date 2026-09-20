using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;
using TroyTechAssessment.Pages;

namespace TroyTechAssessment.Tests;

public sealed class AdditionalApplicantTests
{
    [Fact]
    public async Task CheckApplicantEmail_ReturnsTrue_ForExistingAccountIgnoringCase()
    {
        await using var db = CreateDbContext(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "additional@example.com",
            UserName = "additional@example.com"
        });
        var model = CreateModel(db);

        var result = await model.OnGetCheckApplicantEmailAsync(" ADDITIONAL@EXAMPLE.COM ");

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        Assert.True((bool)json.Value!.GetType().GetProperty("exists")!.GetValue(json.Value)!);
    }

    [Fact]
    public async Task CheckApplicantEmail_ReturnsFalse_ForUnknownAccount()
    {
        await using var db = CreateDbContext();
        var model = CreateModel(db);

        var result = await model.OnGetCheckApplicantEmailAsync("missing@example.com");

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        Assert.False((bool)json.Value!.GetType().GetProperty("exists")!.GetValue(json.Value)!);
    }

    [Fact]
    public void ApplicationApplicant_StoresIdentityOnlyThroughUserId()
    {
        Assert.Null(typeof(ApplicationApplicant).GetProperty("Email"));
        Assert.Equal(typeof(Guid), typeof(ApplicationApplicant).GetProperty(nameof(ApplicationApplicant.UserId))!.PropertyType);
    }

    private static ApplicationDbContext CreateDbContext(params ApplicationUser[] users)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        db.Users.AddRange(users);
        db.SaveChanges();
        return db;
    }

    private static ApplyModel CreateModel(ApplicationDbContext db)
    {
        var model = new ApplyModel(db);
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, "Applicant"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        ], "Test");
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
        return model;
    }
}

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;
using TroyTechAssessment.Pages.Applications;

namespace TroyTechAssessment.Tests;

public sealed class ApplicationListTests
{
    [Fact]
    public async Task ApplicantList_IncludesLinkedApplicantAndTheirPropertyFilter()
    {
        var applicantId = Guid.NewGuid();
        var user = CreateUser(applicantId, "linked@example.com");
        var property = CreateProperty(1, "Linked applicant property");
        var application = CreateApplication(1, property, "Draft");
        application.Applicants.Add(new ApplicationApplicant
        {
            ApplicationId = application.Id,
            UserId = user.Id,
            User = user,
            FirstName = "Linked",
            LastName = "Applicant",
            RowVersion = [1]
        });

        await using var db = CreateDbContext(property, application);
        var model = CreateModel(db, applicantId, "linked@example.com", "Applicant");

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Applications);
        Assert.Contains(model.PropertyOptions, option => option.Text == property.Name);
    }

    [Fact]
    public async Task ManagerList_OnlyIncludesApplicationsForAssignedProperties()
    {
        var managerId = Guid.NewGuid();
        var assignedProperty = CreateProperty(1, "Assigned");
        var otherProperty = CreateProperty(2, "Other");
        var assignedApplication = CreateApplication(1, assignedProperty, "Submitted");
        var otherApplication = CreateApplication(2, otherProperty, "Submitted");

        await using var db = CreateDbContext(
            assignedProperty,
            otherProperty,
            assignedApplication,
            otherApplication,
            new ManagerProperty { ManagerId = managerId, PropertyId = assignedProperty.Id });
        var model = CreateModel(db, managerId, "manager@example.com", "Manager");

        await model.OnGetAsync();

        Assert.Single(model.Applications);
        Assert.Equal(assignedApplication.Id, model.Applications[0].Id);
    }

    [Fact]
    public async Task Search_MatchesAdditionalApplicantFields()
    {
        var applicantId = Guid.NewGuid();
        var property = CreateProperty(1, "Search property");
        var application = CreateApplication(1, property, "Submitted");
        application.Applicants.Add(new ApplicationApplicant
        {
            ApplicationId = application.Id,
            IsPrimary = true,
            UserId = applicantId,
            User = CreateUser(applicantId, "primary@example.com"),
            FirstName = "Primary",
            LastName = "Applicant",
            RowVersion = [1]
        });
        var additionalUserId = Guid.NewGuid();
        application.Applicants.Add(new ApplicationApplicant
        {
            ApplicationId = application.Id,
            UserId = additionalUserId,
            User = CreateUser(additionalUserId, "additional@example.com"),
            FirstName = "Additional",
            LastName = "Applicant",
            PhoneNumber = "555-0100",
            RowVersion = [1]
        });

        await using var db = CreateDbContext(property, application);
        var model = CreateModel(db, applicantId, "primary@example.com", "Applicant");
        model.SearchTerm = "Additional";

        await model.OnGetAsync();

        Assert.Single(model.Applications);
    }

    [Fact]
    public async Task JsonList_UsesCurrentPrimaryApplicantContactValues()
    {
        var applicantId = Guid.NewGuid();
        var property = CreateProperty(1, "JSON property");
        var application = CreateApplication(1, property, "Submitted");
        application.Email = "stale@example.com";
        application.PhoneNumber = "stale-phone";
        application.Applicants.Add(new ApplicationApplicant
        {
            ApplicationId = application.Id,
            IsPrimary = true,
            UserId = applicantId,
            User = CreateUser(applicantId, "current@example.com"),
            FirstName = "Current",
            LastName = "Applicant",
            PhoneNumber = "555-0199",
            RowVersion = [1]
        });

        await using var db = CreateDbContext(property, application);
        var model = CreateModel(db, applicantId, "current@example.com", "Applicant");
        model.PageContext.HttpContext.Request.QueryString = new QueryString("?handler=Json");

        var result = await model.OnGetJsonAsync();

        var json = JsonSerializer.Serialize(((JsonResult)result).Value);
        Assert.Contains("current@example.com", json);
        Assert.Contains("555-0199", json);
        Assert.DoesNotContain("stale@example.com", json);
    }

    [Fact]
    public void ResidenceHistory_IsolatedByApplicantId()
    {
        var application = new Application { Id = 1 };
        var firstApplicant = new ApplicationApplicant { Id = 10, Application = application };
        var secondApplicant = new ApplicationApplicant { Id = 11, Application = application };
        application.ApplicationResidenceHistory.Add(new ApplicationResidenceHistory
        {
            Applicant = firstApplicant,
            ApplicantId = firstApplicant.Id,
            Application = application,
            StreetAddress = "First address"
        });
        application.ApplicationResidenceHistory.Add(new ApplicationResidenceHistory
        {
            Applicant = secondApplicant,
            ApplicantId = secondApplicant.Id,
            Application = application,
            StreetAddress = "Second address"
        });

        var firstHistory = application.ApplicationResidenceHistory
            .Where(history => history.ApplicantId == firstApplicant.Id)
            .ToList();

        Assert.Single(firstHistory);
        Assert.Equal("First address", firstHistory[0].StreetAddress);
    }

    private static ApplicationsModel CreateModel(
        ApplicationDbContext db,
        Guid userId,
        string email,
        string role)
    {
        var model = new ApplicationsModel(db);
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            ],
            "Test");
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
        return model;
    }

    private static ApplicationDbContext CreateDbContext(params object[] entities)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        db.ApplicationStatuses.AddRange(
            new ApplicationStatus { Id = 1, Status = "Draft" },
            new ApplicationStatus { Id = 2, Status = "Submitted" });
        db.SaveChanges();

        foreach (var entity in entities)
        {
            switch (entity)
            {
                case Property property:
                    db.Properties.Add(property);
                    break;
                case Application application:
                    application.ApplicationStatus =
                        db.ApplicationStatuses.Single(status =>
                            status.Id == application.ApplicationStatusId);
                    db.Applications.Add(application);
                    break;
                case ManagerProperty assignment:
                    db.ManagerProperties.Add(assignment);
                    break;
            }
        }

        db.SaveChanges();
        return db;
    }

    private static Property CreateProperty(int id, string name) => new()
    {
        Id = id,
        Name = name,
        StreetAddress = "1 Main St",
        City = "Test City",
        State = "TS",
        ZipCode = "00000"
    };

    private static ApplicationUser CreateUser(Guid id, string email) => new()
    {
        Id = id,
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        FirstName = "Test",
        LastName = "Applicant"
    };

    private static Application CreateApplication(
        int id,
        Property property,
        string status)
    {
        var statusId = status == "Submitted" ? 2 : 1;
        var unit = new Unit
        {
            Id = id,
            Number = $"{id}A",
            Bedrooms = 1,
            MonthlyRent = 1000,
            PropertyId = property.Id,
            Property = property,
            UnitTypeId = 1,
            UnitType = new UnitType { Id = id, UnitTypeName = "Type" }
        };
        property.Units.Add(unit);
        return new Application
        {
            Id = id,
            UserId = Guid.NewGuid(),
            UnitId = unit.Id,
            Unit = unit,
            ApplicationStatusId = statusId,
            ApplicationStatus = new ApplicationStatus { Id = statusId, Status = status },
            RowVersion = [1],
            FirstName = "Primary",
            LastName = "Applicant",
            Email = "primary@example.com",
            PhoneNumber = "555-0101",
            CurrentStreetAddress = "1 Current St",
            CurrentCity = "Test City",
            CurrentState = "TS",
            CurrentZipCode = "00000"
        };
    }
}

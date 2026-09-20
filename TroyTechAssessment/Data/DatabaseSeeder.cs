using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TroyTechAssessment.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureRoleAsync(roleManager, "Manager",
            Guid.Parse("00000000-0000-0000-0000-000000000001"));
        await EnsureRoleAsync(roleManager, "Applicant",
            Guid.Parse("00000000-0000-0000-0000-000000000002"));

        var hasSeedData = await db.Properties.AnyAsync() &&
                          await db.Users.AnyAsync() &&
                          await db.Units.AnyAsync() &&
                          await db.Applications.AnyAsync() &&
                          await db.ApplicationStatusHistory.AnyAsync() &&
                          await db.Leases.AnyAsync();
        if (hasSeedData)
        {
            return;
        }

        var hasPartialData = await db.Properties.AnyAsync() ||
                             await db.Users.AnyAsync() ||
                             await db.Units.AnyAsync() ||
                             await db.Applications.AnyAsync();
        if (hasPartialData)
        {
            throw new InvalidOperationException(
                "The database contains partial seed data. Restore the seed data consistently before startup.");
        }

        var managerUsers = new List<ApplicationUser>();
        for (var i = 1; i <= 2; i++)
        {
            var manager = new Faker<ApplicationUser>("en")
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName())
                .RuleFor(u => u.UserName, f => $"manager{i}_{f.Internet.UserName()}@troytech.local")
                .RuleFor(u => u.Email, (f, u) => u.UserName)
                .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("###-###-####"))
                .Generate();

            managerUsers.Add(manager);
            var result = await userManager.CreateAsync(manager, "P@ssw0rd!");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            var roleResult = await userManager.AddToRoleAsync(manager, "Manager");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        var applicantUsers = new List<ApplicationUser>();
        for (var i = 1; i <= 60; i++)
        {
            var applicant = new Faker<ApplicationUser>("en")
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName())
                .RuleFor(u => u.UserName, f => $"applicant{i}_{f.Internet.UserName()}@troytech.local")
                .RuleFor(u => u.Email, (f, u) => u.UserName)
                .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("###-###-####"))
                .Generate();

            applicantUsers.Add(applicant);
            var result = await userManager.CreateAsync(applicant, "P@ssw0rd!");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            var roleResult = await userManager.AddToRoleAsync(applicant, "Applicant");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        var properties = new[]
        {
            new Property { Name = "Oak Grove Apartments", StreetAddress = "101 Oak Grove Drive", City = "Seattle", State = "WA", ZipCode = "98101" },
            new Property { Name = "Maple Terrace", StreetAddress = "220 Maple Avenue", City = "Tacoma", State = "WA", ZipCode = "98402" },
            new Property { Name = "Pine Crest Homes", StreetAddress = "88 Pine Crest Lane", City = "Bellevue", State = "WA", ZipCode = "98004" }
            ,new Property { Name = "Cedar Valley Residences", StreetAddress = "410 Cedar Valley Road", City = "Redmond", State = "WA", ZipCode = "98052" }
            ,new Property { Name = "Riverstone Commons", StreetAddress = "725 Riverstone Boulevard", City = "Kirkland", State = "WA", ZipCode = "98033" }
        };

        db.Properties.AddRange(properties);
        await db.SaveChangesAsync();

        var savedProperties = await db.Properties.OrderBy(p => p.Id).ToListAsync();
        db.ManagerProperties.AddRange(
            new ManagerProperty { ManagerId = managerUsers[0].Id, PropertyId = savedProperties[0].Id },
            new ManagerProperty { ManagerId = managerUsers[0].Id, PropertyId = savedProperties[1].Id },
            new ManagerProperty { ManagerId = managerUsers[0].Id, PropertyId = savedProperties[3].Id },
            new ManagerProperty { ManagerId = managerUsers[1].Id, PropertyId = savedProperties[1].Id },
            new ManagerProperty { ManagerId = managerUsers[1].Id, PropertyId = savedProperties[2].Id },
            new ManagerProperty { ManagerId = managerUsers[1].Id, PropertyId = savedProperties[4].Id });
        await db.SaveChangesAsync();

        var units = Enumerable.Range(0, 80)
            .Select(index => new Unit
            {
                Number = $"{index / 5 + 1}{index % 5 + 1:00}",
                Bedrooms = index % 4,
                MonthlyRent = 1500 + (index % 10) * 125,
                PropertyId = savedProperties[index % savedProperties.Count].Id,
                UnitTypeId = index % 4 + 1
            })
            .ToList();

        db.Units.AddRange(units);
        await db.SaveChangesAsync();

        var savedUnits = await db.Units.OrderBy(u => u.Id).ToListAsync();
        var applicationStatuses = await db.ApplicationStatuses.OrderBy(s => s.Id).ToListAsync();
        var statusByName = applicationStatuses.ToDictionary(s => s.Status, s => s.Id);

        var applications = applicantUsers.Select((applicant, index) => new Application
        {
            FirstName = applicant.FirstName,
            LastName = applicant.LastName,
            Email = applicant.Email!,
            PhoneNumber = applicant.PhoneNumber!,
            CurrentStreetAddress = $"{100 + index} Cedar Street",
            CurrentCity = savedProperties[index % savedProperties.Count].City,
            CurrentState = "WA",
            CurrentZipCode = savedProperties[index % savedProperties.Count].ZipCode,
            UserId = applicant.Id,
            UnitId = savedUnits[index].Id,
            ApplicationStatusId = statusByName[statusByName.Keys.ElementAt(index % statusByName.Count)]
        }).ToList();

        db.Applications.AddRange(applications);
        await db.SaveChangesAsync();

        db.ApplicationApplicants.AddRange(applications.Select(application => new ApplicationApplicant
        {
            ApplicationId = application.Id,
            UserId = application.UserId,
            FirstName = application.FirstName,
            LastName = application.LastName,
            PhoneNumber = application.PhoneNumber,
            CurrentStreetAddress = application.CurrentStreetAddress,
            CurrentCity = application.CurrentCity,
            CurrentState = application.CurrentState,
            CurrentZipCode = application.CurrentZipCode,
            IsPrimary = true
        }));
        await db.SaveChangesAsync();

        var savedApplications = await db.Applications.OrderBy(a => a.Id).ToListAsync();
        var historyEntries = savedApplications.Select((application, index) => new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            ApplicationStatusId = application.ApplicationStatusId,
            ChangedByUserId = applicantUsers[index].Id,
            ChangedAtUtc = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc).AddDays(index),
            Comment = "Seeded application status history."
        }).ToList();

        db.ApplicationStatusHistory.AddRange(historyEntries);

        db.Leases.AddRange(Enumerable.Range(0, 10).Select(index => new Lease
        {
            UnitId = savedUnits[index].Id,
            ApplicationId = savedApplications[index].Id,
            StartDate = new DateTime(2026, 2, 1).AddMonths(index),
            EndDate = new DateTime(2027, 1, 31).AddMonths(index),
            CreatedAtUtc = new DateTime(2026, 1, 13, 11, 50, 0, DateTimeKind.Utc).AddDays(index)
        }));
        await db.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        string roleName,
        Guid roleId)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is not null)
        {
            return;
        }

        var result = await roleManager.CreateAsync(new IdentityRole<Guid>
        {
            Id = roleId,
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            ConcurrencyStamp = $"{roleName.ToLowerInvariant()}-role-concurrency"
        });
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Tests;

public class LeaseTests
{
    [Fact]
    public void CoversDate_ReturnsTrue_WhenDateFallsWithinLeaseRange()
    {
        var lease = new Lease
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31)
        };

        var result = lease.CoversDate(new DateTime(2025, 6, 15));

        Assert.True(result);
    }

    [Fact]
    public void CoversDate_ReturnsFalse_WhenDateFallsOutsideLeaseRange()
    {
        var lease = new Lease
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31)
        };

        var result = lease.CoversDate(new DateTime(2026, 1, 1));

        Assert.False(result);
    }

    [Fact]
    public void CoversDate_UsesInclusiveBoundaryDates()
    {
        var lease = new Lease
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31)
        };

        Assert.True(lease.CoversDate(lease.StartDate));
        Assert.True(lease.CoversDate(lease.EndDate));
    }
}

public class UnitTests
{
    [Fact]
    public void BedroomOptions_ReturnsExpectedValues()
    {
        var values = Unit.BedroomOptions;

        Assert.Equal(new[] { 0, 1, 2, 3 }, values);
    }
}

public class LeaseTermTests
{
    [Fact]
    public void CalculateEndDate_ReturnsTwelveMonthInclusiveTerm()
    {
        var start = new DateTime(2026, 9, 18);

        var end = Lease.CalculateEndDate(start);

        Assert.Equal(new DateTime(2027, 9, 17), end);
    }

    [Fact]
    public void Overlaps_ReturnsTrue_WhenTermsShareAnEndpoint()
    {
        var lease = new Lease
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };

        Assert.True(lease.Overlaps(new DateTime(2026, 12, 31), new DateTime(2027, 12, 30)));
    }

    [Fact]
    public void Overlaps_ReturnsFalse_WhenTermStartsAfterLeaseEnds()
    {
        var lease = new Lease
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };

        Assert.False(lease.Overlaps(new DateTime(2027, 1, 1), new DateTime(2027, 12, 31)));
    }
}

public class ResidenceHistoryTests
{
    [Fact]
    public void HasValidDateRange_AllowsOpenEndedCurrentResidence()
    {
        Assert.True(ApplicationResidenceHistory.HasValidDateRange(
            new DateTime(2025, 1, 1), null));
    }

    [Fact]
    public void HasValidDateRange_RejectsMoveOutBeforeMoveIn()
    {
        Assert.False(ApplicationResidenceHistory.HasValidDateRange(
            new DateTime(2025, 6, 1),
            new DateTime(2025, 5, 31)));
    }

    [Fact]
    public void Overlaps_TreatsOpenEndedResidenceAsActive()
    {
        var current = new ApplicationResidenceHistory
        {
            MoveInDate = new DateTime(2025, 1, 1)
        };
        var later = new ApplicationResidenceHistory
        {
            MoveInDate = new DateTime(2026, 1, 1),
            MoveOutDate = new DateTime(2026, 12, 31)
        };

        Assert.True(current.Overlaps(later));
    }
}

public class ApplicationWorkflowTests
{
    [Fact]
    public void Application_UsesRowVersionForConcurrentUpdates()
    {
        var property = typeof(Application).GetProperty(nameof(Application.RowVersion));

        Assert.NotNull(property);
        Assert.Equal(typeof(byte[]), property.PropertyType);
        Assert.NotNull(property!.GetCustomAttribute<TimestampAttribute>());
    }

    [Fact]
    public void NewApplication_StartsUnclaimedWithInitializedCollections()
    {
        var application = new Application();

        Assert.Null(application.ClaimedByManagerId);
        Assert.Null(application.ClaimedAtUtc);
        Assert.Empty(application.ManagerNotes);
        Assert.Empty(application.StatusHistory);
        Assert.Empty(application.ApplicationResidenceHistory);
    }

    [Fact]
    public void ManagerNote_RequiresTextAndLimitsTextTo4000Characters()
    {
        var property = typeof(ManagerNote).GetProperty(nameof(ManagerNote.Notes));

        Assert.NotNull(property);
        Assert.NotNull(property!.GetCustomAttribute<RequiredAttribute>());
        Assert.Equal(4000, property.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength);
    }

    [Fact]
    public void ManagerNote_StoresApplicationAndManagerOwnership()
    {
        var application = new Application();
        var manager = new ApplicationUser();
        var note = new ManagerNote
        {
            Application = application,
            Manager = manager,
            Notes = "Follow up with applicant."
        };

        application.ManagerNotes.Add(note);
        manager.ManagerNotes.Add(note);

        Assert.Same(application, note.Application);
        Assert.Same(manager, note.Manager);
        Assert.Contains(note, application.ManagerNotes);
        Assert.Contains(note, manager.ManagerNotes);
    }
}

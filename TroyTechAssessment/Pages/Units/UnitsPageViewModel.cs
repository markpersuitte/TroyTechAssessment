using TroyTechAssessment.Data;

namespace TroyTechAssessment.Pages.Units;

public sealed class UnitsPageViewModel
{
    public IReadOnlyList<Unit> Units { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    public string Sort { get; init; } = "property";
    public string Direction { get; init; } = "asc";
    public int? PropertyId { get; init; }
    public IReadOnlyList<Property> PropertyOptions { get; init; } = [];
    public bool CanManageUnits { get; init; }
    public bool CanApply { get; init; }
}

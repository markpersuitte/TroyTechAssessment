using Microsoft.AspNetCore.Mvc;
namespace TroyTechAssessment.ViewComponents;

public sealed class PagedGridViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(
        int page,
        int pageSize,
        int totalItems,
        string routeUrl)
    {
        return View(new PagedGridModel(page, pageSize, totalItems, routeUrl));
    }

    public sealed record PagedGridModel(
        int Page,
        int PageSize,
        int TotalItems,
        string RouteUrl)
    {
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
        public int FirstItem => TotalItems == 0 ? 0 : ((Page - 1) * PageSize) + 1;
        public int LastItem => Math.Min(Page * PageSize, TotalItems);
    }
}

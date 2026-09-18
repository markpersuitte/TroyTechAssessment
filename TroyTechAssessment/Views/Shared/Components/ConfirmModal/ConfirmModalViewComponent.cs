using Microsoft.AspNetCore.Mvc;

namespace TroyTechAssessment.ViewComponents;

public class ConfirmModalViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string modalId, string text, string action, string page = "/Index",
        int? unitId = null, int? propertyId = null, int? unitTypeId = null, bool informational = false,
        string? controller = null, string? controllerAction = null)
    {
        return View(new ConfirmModalModel(
            modalId, text, action, page, unitId, propertyId, unitTypeId, informational,
            controller, controllerAction));
    }

    public sealed record ConfirmModalModel(string ModalId, string Text, string Action, string Page,
        int? UnitId, int? PropertyId, int? UnitTypeId, bool Informational,
        string? Controller, string? ControllerAction);
}

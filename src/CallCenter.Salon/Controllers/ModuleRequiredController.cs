using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class ModuleRequiredController : SlnBaseController
{
    public IActionResult Index(int? moduleId, string? returnUrl = null)
    {
        var module = moduleId.HasValue ? SalonPortalModules.GetById(moduleId.Value) : null;

        ViewData["Title"] = "Hizmet Paketi Gerekli";
        ViewData["ModuleId"] = moduleId;
        ViewData["ModuleName"] = module?.Description ?? module?.SystemName ?? "Bu hizmet";
        ViewData["ModuleIcon"] = module?.Icon ?? "bi-puzzle";
        ViewData["ReturnUrl"] = returnUrl;

        return View();
    }
}

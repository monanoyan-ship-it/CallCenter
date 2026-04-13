using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Abonelik yok / askida / iptal durumunda salonu bu sayfaya yonlendirir.
/// Sadece Home ve bu controller SlnBaseController.OnActionExecuting'de muaf tutulur.
/// </summary>
public class SubscriptionRequiredController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Abonelik Gerekli";
        return View();
    }
}

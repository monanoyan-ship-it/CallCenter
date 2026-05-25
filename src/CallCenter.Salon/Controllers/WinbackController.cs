using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class WinbackController : SlnBaseController
{
    public IActionResult Index()
    {
        if (MarketingRouteAccess.CanUseConsolidated(HttpContext))
            return RedirectToAction("Index", "Marketing", new { tab = "winback" });

        return View();
    }
}

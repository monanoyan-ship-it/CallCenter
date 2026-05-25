using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class LoyaltyController : SlnBaseController
{
    public IActionResult Index()
    {
        if (MarketingRouteAccess.CanUseConsolidated(HttpContext))
            return RedirectToAction("Index", "Marketing", new { tab = "loyalty" });

        return View();
    }
}

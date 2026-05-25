using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class MembershipsController : SlnBaseController
{
    public IActionResult Index()
    {
        if (MarketingRouteAccess.CanUseConsolidated(HttpContext))
            return RedirectToAction("Index", "Marketing", new { tab = "memberships" });

        return View();
    }
}

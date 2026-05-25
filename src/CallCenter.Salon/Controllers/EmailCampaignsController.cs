using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class EmailCampaignsController : SlnBaseController
{
    public IActionResult Index()
    {
        if (MarketingRouteAccess.CanUseConsolidated(HttpContext))
            return RedirectToAction("Index", "Marketing", new { tab = "email" });

        return View();
    }
}

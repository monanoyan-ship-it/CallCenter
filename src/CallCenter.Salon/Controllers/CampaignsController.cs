namespace CallCenter.Salon.Controllers;

public class CampaignsController : SlnBaseController
{
    public IActionResult Index()
    {
        if (MarketingRouteAccess.CanUseConsolidated(HttpContext))
            return RedirectToAction("Index", "Marketing", new { tab = "campaigns" });

        return View();
    }
}

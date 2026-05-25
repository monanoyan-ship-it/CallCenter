using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class ReviewsController : SlnBaseController
{
    public IActionResult Index()
    {
        if (MarketingRouteAccess.CanUseConsolidated(HttpContext))
            return RedirectToAction("Index", "Marketing", new { tab = "reviews" });

        return View();
    }
}

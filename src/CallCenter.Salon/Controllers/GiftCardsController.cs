using Microsoft.AspNetCore.Mvc;
namespace CallCenter.Salon.Controllers;

public class GiftCardsController : SlnBaseController
{
    public IActionResult Index()
    {
        if (MarketingRouteAccess.CanUseConsolidated(HttpContext))
            return RedirectToAction("Index", "Marketing", new { tab = "giftcards" });

        return View();
    }
}

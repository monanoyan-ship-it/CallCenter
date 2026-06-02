using Microsoft.AspNetCore.Mvc;
namespace CallCenter.Salon.Controllers;

public class GiftCardsController : SlnBaseController
{
    public IActionResult Index()
    {
        return MarketingRouteAccess.RedirectToCrm(this, "/SalonCrm/GiftCards");
    }
}

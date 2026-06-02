using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class ReviewsController : SlnBaseController
{
    public IActionResult Index()
    {
        return MarketingRouteAccess.RedirectToCrm(this, "/SalonCrm/Reviews");
    }
}

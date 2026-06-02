using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class WinbackController : SlnBaseController
{
    public IActionResult Index()
    {
        return MarketingRouteAccess.RedirectToCrm(this, "/SalonCrm/Winback");
    }
}

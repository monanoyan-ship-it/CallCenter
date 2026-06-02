using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class EmailCampaignsController : SlnBaseController
{
    public IActionResult Index()
    {
        return MarketingRouteAccess.RedirectToCrm(this, "/SalonCrm/EmailCampaigns");
    }
}

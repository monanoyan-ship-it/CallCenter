namespace CallCenter.Salon.Controllers;

public class CampaignsController : SlnBaseController
{
    public IActionResult Index()
    {
        return MarketingRouteAccess.RedirectToCrm(this, "/SalonCrm/Campaigns");
    }
}

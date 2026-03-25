namespace CallCenter.Salon.Controllers;

public class CampaignsController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Kampanyalar";
        return View();
    }
}

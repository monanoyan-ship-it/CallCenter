using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class EmailCampaignsController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "E-posta Kampanyalari";
        return View();
    }
}

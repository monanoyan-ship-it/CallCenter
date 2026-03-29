using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class WaitlistController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Bekleme Listesi";
        return View();
    }
}

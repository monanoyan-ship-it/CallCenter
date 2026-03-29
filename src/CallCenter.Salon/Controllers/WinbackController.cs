using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class WinbackController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Musteri Geri Kazanim";
        return View();
    }
}

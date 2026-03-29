using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class NoShowPolicyController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "No-Show Politikasi";
        return View();
    }
}

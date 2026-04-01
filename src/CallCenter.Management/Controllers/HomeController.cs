using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class HomeController : MgmtBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }
}

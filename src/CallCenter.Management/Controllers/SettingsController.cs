using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class SettingsController : MgmtBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Sistem Ayarlari";
        return View();
    }
}

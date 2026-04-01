using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class ModulesController : MgmtBaseController
{
    public IActionResult CallCenter()
    {
        ViewData["Title"] = "CC Modul Paneli";
        return View();
    }

    public IActionResult Salon()
    {
        ViewData["Title"] = "Salon Modul Paneli";
        return View();
    }
}

using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class ModulesController : MgmtBaseController
{
    public IActionResult CallCenter()
    {
        ViewData["Title"] = "CC Odeme Takibi";
        return View();
    }

    public IActionResult Salon()
    {
        ViewData["Title"] = "Salon Odeme Takibi";
        return View();
    }
}

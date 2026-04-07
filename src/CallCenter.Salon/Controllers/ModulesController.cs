using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class ModulesController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Modul Yonetimi";
        return View();
    }
}

using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class DataImportController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Veri Aktarimi";
        return View();
    }
}

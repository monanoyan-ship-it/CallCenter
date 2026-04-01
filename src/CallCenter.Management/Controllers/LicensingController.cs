using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class LicensingController : MgmtBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Lisanslama / Paketler";
        return View();
    }
}

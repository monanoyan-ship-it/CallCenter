using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class CustomersController : MgmtBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Musteriler";
        return View();
    }
}

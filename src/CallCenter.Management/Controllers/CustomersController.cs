using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class CustomersController : MgmtBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Müşteriler";
        return View();
    }

    public IActionResult Detail(int id)
    {
        ViewData["Title"] = "Müşteri Detay";
        ViewData["CustomerId"] = id;
        return View();
    }
}

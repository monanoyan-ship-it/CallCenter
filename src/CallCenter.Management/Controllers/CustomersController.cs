using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class CustomersController : MgmtBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Musteriler";
        return View();
    }

    public IActionResult Detail(int id)
    {
        ViewData["Title"] = "Musteri Detay";
        ViewData["CustomerId"] = id;
        return View();
    }
}

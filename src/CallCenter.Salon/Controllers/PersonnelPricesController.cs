using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class PersonnelPricesController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Personel Fiyatlari & Hasilat";
        return View();
    }
}

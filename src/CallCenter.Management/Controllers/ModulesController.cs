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

    public IActionResult Pricing()
    {
        ViewData["Title"] = "Modul Fiyatlari";
        return View();
    }

    public IActionResult PricingPeriods()
    {
        ViewData["Title"] = "Fiyat Dönemleri";
        return View();
    }

    public IActionResult Requests()
    {
        ViewData["Title"] = "Modul Talepleri";
        return View();
    }

    public IActionResult Inventory()
    {
        ViewData["Title"] = "Modul Envanteri";
        return View();
    }

    public IActionResult Roles()
    {
        ViewData["Title"] = "Salon Rol Yonetimi";
        return View();
    }

    public IActionResult Subscriptions()
    {
        ViewData["Title"] = "Abonelik Yonetimi";
        return View();
    }
}

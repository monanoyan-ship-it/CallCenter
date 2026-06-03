using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class ModulesController : MgmtBaseController
{
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Inventory));
    }

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

    public IActionResult Crm()
    {
        ViewData["Title"] = "CRM Odeme Takibi";
        ViewData["BillingProductTypeId"] = 3;
        return View("CallCenter");
    }

    public IActionResult Pricing()
    {
        ViewData["Title"] = "Hizmet Fiyatları";
        return RedirectToAction(nameof(PricingPeriods));
    }

    public IActionResult PricingPeriods()
    {
        ViewData["Title"] = "Fiyat Dönemleri";
        return View();
    }

    public IActionResult Requests()
    {
        ViewData["Title"] = "Hizmet Talepleri";
        return View();
    }

    public IActionResult Inventory()
    {
        ViewData["Title"] = "Hizmet Envanteri";
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

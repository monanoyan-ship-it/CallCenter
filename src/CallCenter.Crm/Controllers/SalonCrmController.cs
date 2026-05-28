using CallCenter.Shared.Enums;

namespace CallCenter.Crm.Controllers;

public class SalonCrmController : CrmBaseController
{
    public IActionResult Loyalty()
    {
        if (RequireCrmScope(CrmScopes.Salon) is { } denied) return denied;

        ViewData["Title"] = "Salon - Sadakat";
        return View();
    }

    public IActionResult Memberships()
    {
        if (RequireCrmScope(CrmScopes.Salon) is { } denied) return denied;

        ViewData["Title"] = "Salon - Uyelikler";
        return View();
    }

    public IActionResult GiftCards()
    {
        if (RequireCrmScope(CrmScopes.Salon) is { } denied) return denied;

        ViewData["Title"] = "Salon - Hediye Kartlari";
        return View();
    }

    public IActionResult Campaigns()
    {
        if (RequireCrmScope(CrmScopes.Salon) is { } denied) return denied;

        ViewData["Title"] = "Salon - Pazarlama ve SMS";
        return View();
    }

    public IActionResult EmailCampaigns()
    {
        if (RequireCrmScope(CrmScopes.Salon) is { } denied) return denied;

        ViewData["Title"] = "Salon - E-posta Kampanyalari";
        return View();
    }

    public IActionResult Reviews()
    {
        if (RequireCrmScope(CrmScopes.Salon) is { } denied) return denied;

        ViewData["Title"] = "Salon - Yorum Yonetimi";
        return View();
    }

    public IActionResult Winback()
    {
        if (RequireCrmScope(CrmScopes.Salon) is { } denied) return denied;

        ViewData["Title"] = "Salon - Kayip Musteri Geri Kazanim";
        return View();
    }
}

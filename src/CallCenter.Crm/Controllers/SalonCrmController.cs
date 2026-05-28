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
}

namespace CallCenter.Crm.Controllers;

public class IntegrationsController : CrmBaseController
{
    public IActionResult Index()
    {
        if (RequireCoreOrCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Entegrasyonlar";
        return View();
    }

    public IActionResult Connect(string platform)
    {
        if (RequireCoreOrCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Entegrasyon Baglantisi";
        ViewData["Platform"] = platform ?? "";
        return View();
    }

    public IActionResult Detail(string uid)
    {
        if (RequireCoreOrCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Entegrasyon Detay";
        ViewData["ConnectionUid"] = uid ?? "";
        return View();
    }

    public IActionResult Webhooks()
    {
        if (RequireCoreOrCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Webhook Yonetimi";
        return View();
    }
}

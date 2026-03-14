namespace CallCenter.Crm.Controllers;

public class IntegrationsController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Entegrasyonlar";
        return View();
    }

    public IActionResult Connect(string platform)
    {
        ViewData["Title"] = "Entegrasyon Baglantisi";
        ViewData["Platform"] = platform ?? "";
        return View();
    }

    public IActionResult Detail(string uid)
    {
        ViewData["Title"] = "Entegrasyon Detay";
        ViewData["ConnectionUid"] = uid ?? "";
        return View();
    }

    public IActionResult Webhooks()
    {
        ViewData["Title"] = "Webhook Yonetimi";
        return View();
    }
}

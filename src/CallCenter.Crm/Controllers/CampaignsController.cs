namespace CallCenter.Crm.Controllers;

public class CampaignsController : CrmBaseController
{
    public IActionResult Index()
    {
        if (RequireCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Arama Kampanyalari";
        return View();
    }

    public IActionResult Detail(Guid uid)
    {
        if (RequireCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Kampanya Detay";
        ViewData["CampaignUid"] = uid.ToString();
        return View();
    }
}

namespace CallCenter.Crm.Controllers;

public class CampaignsController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Arama Kampanyalari";
        return View();
    }

    public IActionResult Detail(Guid uid)
    {
        ViewData["Title"] = "Kampanya Detay";
        ViewData["CampaignUid"] = uid.ToString();
        return View();
    }
}

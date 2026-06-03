namespace CallCenter.Crm.Controllers;

public class ReportsController : CrmBaseController
{
    public IActionResult Index()
    {
        if (RequireCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Raporlar";
        return View();
    }
}

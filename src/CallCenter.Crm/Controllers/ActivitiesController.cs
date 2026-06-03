namespace CallCenter.Crm.Controllers;

public class ActivitiesController : CrmBaseController
{
    public IActionResult Index()
    {
        if (RequireCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Etkilesimler";
        return View();
    }
}

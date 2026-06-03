namespace CallCenter.Crm.Controllers;

public class TasksController : CrmBaseController
{
    public IActionResult Index()
    {
        if (RequireCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Gorevler";
        return View();
    }
}

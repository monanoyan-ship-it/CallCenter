namespace CallCenter.Crm.Controllers;

public class TicketsController : CrmBaseController
{
    public IActionResult Index()
    {
        if (RequireCoreOrCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Talepler";
        return View();
    }
}

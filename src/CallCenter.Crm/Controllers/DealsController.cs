namespace CallCenter.Crm.Controllers;

public class DealsController : CrmBaseController
{
    public IActionResult Index()
    {
        if (RequireCoreCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Firsatlar";
        return View();
    }
}

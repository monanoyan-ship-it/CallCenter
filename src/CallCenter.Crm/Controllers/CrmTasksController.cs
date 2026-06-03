using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Crm.Controllers;

public class CrmTasksController : CrmBaseController
{
    public IActionResult Index()
    {
        if (RequireCoreCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Gorevler";
        return View();
    }
}

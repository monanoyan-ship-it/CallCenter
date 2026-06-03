using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Crm.Controllers;

public class SurveysController : CrmBaseController
{
    public IActionResult Index()
    {
        if (RequireCoreCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Anketler";
        return View();
    }
}

using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Crm.Controllers;

public class SurveysController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Anketler";
        return View();
    }
}

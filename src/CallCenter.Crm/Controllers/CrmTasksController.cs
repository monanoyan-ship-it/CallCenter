using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Crm.Controllers;

public class CrmTasksController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Gorevler";
        return View();
    }
}

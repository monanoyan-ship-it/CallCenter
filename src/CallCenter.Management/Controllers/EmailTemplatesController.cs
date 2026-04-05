using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class EmailTemplatesController : MgmtBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Email Taslaklari";
        return View();
    }
}

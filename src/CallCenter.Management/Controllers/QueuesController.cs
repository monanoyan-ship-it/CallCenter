using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class QueuesController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "Kuyruklar"; return View(); }
}

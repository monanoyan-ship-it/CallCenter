using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class CallForwardingController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "Arama Yonlendirme"; return View(); }
}

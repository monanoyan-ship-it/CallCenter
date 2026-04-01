using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class GatewayHealthController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "Gateway Durumu"; return View(); }
}

using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class SipAccountsController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "SIP Hesaplari"; return View(); }
}

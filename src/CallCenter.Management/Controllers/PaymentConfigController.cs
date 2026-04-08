using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class PaymentConfigController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "Odeme Ayarlari"; return View(); }
}

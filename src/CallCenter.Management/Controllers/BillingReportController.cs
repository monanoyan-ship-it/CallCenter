using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class BillingReportController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "Faturalama Raporu"; return View(); }
}

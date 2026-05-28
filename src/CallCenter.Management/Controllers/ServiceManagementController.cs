using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class ServiceManagementController : MgmtBaseController
{
    public IActionResult Index()
    {
        return RedirectToAction("PricingPeriods", "Modules");
    }
}

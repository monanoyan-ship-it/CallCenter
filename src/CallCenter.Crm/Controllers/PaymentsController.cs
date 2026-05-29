using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Crm.Controllers;

public class PaymentsController : CrmBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}

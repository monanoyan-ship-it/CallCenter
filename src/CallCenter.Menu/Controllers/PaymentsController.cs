using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Menu.Controllers;

public class PaymentsController : MenuBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}

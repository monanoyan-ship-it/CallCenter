using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Menu.Controllers;

public class OrdersController : MenuBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}

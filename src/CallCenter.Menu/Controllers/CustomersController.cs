using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Menu.Controllers;

public class CustomersController : MenuBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}

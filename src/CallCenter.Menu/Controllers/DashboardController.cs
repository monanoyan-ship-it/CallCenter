using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Menu.Controllers;

public class DashboardController : MenuBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}

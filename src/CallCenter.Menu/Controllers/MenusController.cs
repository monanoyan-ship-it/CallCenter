using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Menu.Controllers;

public class MenusController : MenuBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}

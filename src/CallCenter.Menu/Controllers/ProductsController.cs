using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Menu.Controllers;

public class ProductsController : MenuBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}

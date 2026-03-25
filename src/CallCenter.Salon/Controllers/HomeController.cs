namespace CallCenter.Salon.Controllers;

public class HomeController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }
}

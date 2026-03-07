namespace CallCenter.Crm.Controllers;

public class HomeController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }
}

namespace CallCenter.Salon.Controllers;

public class ServicesController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Hizmetler";
        return View();
    }
}

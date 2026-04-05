namespace CallCenter.Salon.Controllers;

public class EmailSettingsController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "E-posta Ayarlari";
        return View();
    }
}

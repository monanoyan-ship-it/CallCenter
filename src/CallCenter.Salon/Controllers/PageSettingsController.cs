namespace CallCenter.Salon.Controllers;

public class PageSettingsController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Sayfa Ayarlari";
        return View();
    }
}

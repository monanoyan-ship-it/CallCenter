namespace CallCenter.Salon.Controllers;

public class CashController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Kasa";
        return View();
    }
}

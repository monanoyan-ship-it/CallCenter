namespace CallCenter.Salon.Controllers;

public class ReportsController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Raporlar";
        return View();
    }
}

namespace CallCenter.Salon.Controllers;

public class InvoicesController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Adisyonlar";
        return View();
    }
}

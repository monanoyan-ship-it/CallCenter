namespace CallCenter.Salon.Controllers;

public class SuppliersController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Tedarikciler";
        return View();
    }
}

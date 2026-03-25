namespace CallCenter.Salon.Controllers;

public class StaffController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Personel";
        return View();
    }
}

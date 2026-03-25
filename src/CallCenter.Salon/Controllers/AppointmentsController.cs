namespace CallCenter.Salon.Controllers;

public class AppointmentsController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Randevular";
        return View();
    }
}

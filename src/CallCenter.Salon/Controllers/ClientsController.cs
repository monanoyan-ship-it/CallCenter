namespace CallCenter.Salon.Controllers;

public class ClientsController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Musteriler";
        return View();
    }

    public IActionResult Detail(int id)
    {
        ViewData["Title"] = "Musteri Detay";
        ViewData["ClientId"] = id;
        return View();
    }
}

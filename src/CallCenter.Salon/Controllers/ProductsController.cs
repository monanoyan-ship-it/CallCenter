namespace CallCenter.Salon.Controllers;

public class ProductsController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Urunler";
        return View();
    }
}

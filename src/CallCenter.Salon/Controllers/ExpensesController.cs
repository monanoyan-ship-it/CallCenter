namespace CallCenter.Salon.Controllers;

public class ExpensesController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Masraflar";
        return View();
    }
}

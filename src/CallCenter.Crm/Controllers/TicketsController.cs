namespace CallCenter.Crm.Controllers;

public class TicketsController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Talepler";
        return View();
    }
}

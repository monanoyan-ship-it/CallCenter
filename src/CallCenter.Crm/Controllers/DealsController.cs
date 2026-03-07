namespace CallCenter.Crm.Controllers;

public class DealsController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Firsatlar";
        return View();
    }
}

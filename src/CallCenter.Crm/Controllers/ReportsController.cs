namespace CallCenter.Crm.Controllers;

public class ReportsController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Raporlar";
        return View();
    }
}

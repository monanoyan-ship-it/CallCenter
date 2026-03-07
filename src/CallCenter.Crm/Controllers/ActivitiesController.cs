namespace CallCenter.Crm.Controllers;

public class ActivitiesController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Etkilesimler";
        return View();
    }
}

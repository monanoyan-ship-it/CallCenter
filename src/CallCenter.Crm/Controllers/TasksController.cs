namespace CallCenter.Crm.Controllers;

public class TasksController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Gorevler";
        return View();
    }
}

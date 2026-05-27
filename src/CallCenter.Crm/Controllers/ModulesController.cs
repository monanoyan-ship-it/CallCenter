namespace CallCenter.Crm.Controllers;

public class ModulesController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "CRM Hizmet Kapsamı";
        return View();
    }
}

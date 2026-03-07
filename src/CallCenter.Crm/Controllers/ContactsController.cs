namespace CallCenter.Crm.Controllers;

public class ContactsController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Kisiler";
        return View();
    }
}

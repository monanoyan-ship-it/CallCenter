namespace CallCenter.Crm.Controllers;

public class ContactsController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Kisiler";
        return View();
    }

    public IActionResult Detail(int id)
    {
        ViewData["Title"] = "Kisi Detay";
        ViewData["ContactId"] = id;
        return View();
    }
}

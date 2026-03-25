namespace CallCenter.Crm.Controllers;

public class CrmContactsController : CrmBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Kisiler";
        return View();
    }

    public IActionResult Detail(int id)
    {
        ViewData["Title"] = "Kisi Detay";
        ViewData["CrmContactId"] = id;
        return View();
    }
}

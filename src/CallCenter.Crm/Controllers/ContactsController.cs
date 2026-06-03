namespace CallCenter.Crm.Controllers;

public class ContactsController : CrmBaseController
{
    public IActionResult Index()
    {
        if (RequireCoreOrCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Kisiler";
        return View();
    }

    public IActionResult Detail(int id)
    {
        if (RequireCoreOrCallCenterCrmScope() is { } denied) return denied;

        ViewData["Title"] = "Kisi Detay";
        ViewData["CrmContactId"] = id;
        return View();
    }
}

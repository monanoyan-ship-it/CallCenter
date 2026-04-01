using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class NotificationsController : MgmtBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Toplu Bildirimler";
        return View();
    }
}

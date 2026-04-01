using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class UsersController : MgmtBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Kullanicilar";
        return View();
    }
}

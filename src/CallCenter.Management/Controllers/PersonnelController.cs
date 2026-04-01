using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class PersonnelController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "Personel Yonetimi"; return View(); }
}

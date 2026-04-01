using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class OrganizationsController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "Organizasyonlar"; return View(); }
}

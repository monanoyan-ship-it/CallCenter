using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class IvrController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "IVR Yonetimi"; return View(); }
}

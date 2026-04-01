using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class TranslationsController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "Dil Yonetimi"; return View(); }
}

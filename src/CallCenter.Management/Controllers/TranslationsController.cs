using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class TranslationsController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "Dil Yönetimi"; return View(); }
}

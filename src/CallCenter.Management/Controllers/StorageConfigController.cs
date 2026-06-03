using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class StorageConfigController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "Depolama Yapılandırması"; return View(); }
}

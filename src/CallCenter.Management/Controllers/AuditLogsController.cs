using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class AuditLogsController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "Denetim Kayitlari"; return View(); }
}

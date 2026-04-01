using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class KvkkController : MgmtBaseController
{
    public IActionResult Index() { ViewData["Title"] = "KVKK Dashboard"; return View(); }
    public IActionResult PrivacyNotices() { ViewData["Title"] = "Aydinlatma Metinleri"; return View(); }
    public IActionResult Consents() { ViewData["Title"] = "Riza Yonetimi"; return View(); }
    public IActionResult Requests() { ViewData["Title"] = "Basvurular"; return View(); }
    public IActionResult Breaches() { ViewData["Title"] = "Ihlal Bildirimleri"; return View(); }
    public IActionResult Transfers() { ViewData["Title"] = "Yurt Disi Aktarim"; return View(); }
    public IActionResult Inventory() { ViewData["Title"] = "Veri Envanteri"; return View(); }
    public IActionResult KvkkSettings() { ViewData["Title"] = "KVKK Ayarlari"; return View(); }
}

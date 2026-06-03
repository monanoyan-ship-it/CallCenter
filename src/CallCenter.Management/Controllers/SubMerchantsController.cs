using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

/// <summary>
/// PS.12 — Tüm salonların iyzico Pazaryeri üye işyeri durumu.
/// View knockout ile /proxy/management/sub-merchants endpoint'ini çekiyor.
/// </summary>
public class SubMerchantsController : MgmtBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Pazaryeri Üye İşyerleri";
        return View();
    }
}

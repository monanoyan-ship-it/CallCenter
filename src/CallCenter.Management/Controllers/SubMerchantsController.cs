using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

/// <summary>
/// PS.12 — Tum salonlarin iyzico Pazaryeri sub-merchant durumu.
/// View knockout ile /proxy/management/sub-merchants endpoint-ini cekiyor.
/// </summary>
public class SubMerchantsController : MgmtBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Sub-Merchants (iyzico Pazaryeri)";
        return View();
    }
}

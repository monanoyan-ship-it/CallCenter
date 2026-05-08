using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// PS.5 — Salon iyzico Pazaryeri sub-merchant onboarding sayfasi (sadece SalonOwner).
/// Form submit JS uzerinden /proxy/payments/sub-merchant'e gider.
/// Mevcut onboarding durumu /proxy/sln-profile/payment-info'dan okunur.
/// </summary>
public class PaymentInfoController : SlnBaseController
{
    public IActionResult Index() => View();
}

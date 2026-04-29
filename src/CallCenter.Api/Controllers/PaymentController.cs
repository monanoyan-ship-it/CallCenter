using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using CallCenter.Api.Services;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PaymentController(PaymentService paymentService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration = configuration;
    }

    /// <summary>Salon adisyon odemesi (platform kullanicisi odiyor)</summary>
    [HttpPost("invoice")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult> PayInvoice([FromBody] InvoicePaymentRequest request)
    {
        var platformUserId = GetPlatformUserId();
        var result = await _paymentService.ProcessInvoicePaymentAsync(
            request.CustomerId, request.InvoiceId, platformUserId, request.Card);

        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(new { transactionId = result.TransactionUid, providerTxId = result.ProviderTransactionId });
    }

    /// <summary>Platform abonelik odemesi (firma admin odiyor)</summary>
    [HttpPost("billing")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> PayBilling([FromBody] BillingPaymentRequest request)
    {
        var result = await _paymentService.ProcessBillingPaymentAsync(request.BillingPeriodId, request.Card);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(new { transactionId = result.TransactionUid, providerTxId = result.ProviderTransactionId });
    }

    /// <summary>Modul satin alma (Salon admin KK ile odiyor)</summary>
    [HttpPost("module-purchase")]
    [Authorize(Roles = "CustomerUser")]
    public async Task<ActionResult> PurchaseModule([FromBody] ModulePurchaseRequest request)
    {
        var customerId = GetCustomerId();
        var buyerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _paymentService.ProcessModulePurchaseAsync(customerId, request.ModuleId, request.Card, buyerIp);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(new { transactionId = result.TransactionUid, providerTxId = result.ProviderTransactionId, message = "Modul basariyla satin alindi." });
    }

    /// <summary>Havale ile modul talebi (beklemede kayit olusturur)</summary>
    [HttpPost("havale-request")]
    [Authorize(Roles = "CustomerUser")]
    public async Task<ActionResult> HavaleRequest([FromBody] HavaleRequestDto request)
    {
        var customerId = GetCustomerId();
        var result = await _paymentService.CreateHavaleRequestAsync(customerId, request.ModuleId);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(new { transactionId = result.TransactionUid, message = "Havale talebiniz alindi. Odemeniz onaylandiktan sonra modul aktif edilecektir." });
    }

    /// <summary>Admin havale onaylar</summary>
    [HttpPost("havale-confirm/{txUid:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ConfirmHavale(Guid txUid)
    {
        var result = await _paymentService.ConfirmHavaleAsync(txUid);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(new { message = "Havale onaylandi ve modul aktif edildi." });
    }

    /// <summary>Admin havale reddeder</summary>
    [HttpPost("havale-reject/{txUid:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> RejectHavale(Guid txUid, [FromBody] HavaleRejectDto? dto = null)
    {
        var result = await _paymentService.RejectHavaleAsync(txUid, dto?.Reason);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(new { message = "Havale reddedildi." });
    }

    /// <summary>Bekleyen havale islemleri (Admin)</summary>
    [HttpGet("pending-havale")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetPendingHavale()
    {
        var list = await _paymentService.GetPendingHavaleAsync();
        return Ok(list.Select(t => new
        {
            t.Uid,
            t.PaymentTypeId,
            PaymentType = PaymentTypes.GetById(t.PaymentTypeId)?.Description,
            t.CustomerId,
            CustomerName = t.Customer?.Name,
            t.ModuleId,
            t.Amount,
            t.Currency,
            t.CreatedAt
        }));
    }

    /// <summary>Uyelik odemesi (PlatformUser)</summary>
    [HttpPost("membership")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult> PayMembership([FromBody] MembershipPaymentRequest request)
    {
        var platformUserId = GetPlatformUserId();
        var buyerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _paymentService.ProcessMembershipPaymentAsync(
            request.PlanId, request.SlnClientId, platformUserId, request.Card, buyerIp);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(new { transactionId = result.TransactionUid, message = "Uyelik odemesi basarili." });
    }

    /// <summary>Paket pro-rata on izleme (fiyat hesabi)</summary>
    [HttpPost("package-preview")]
    [Authorize(Roles = "CustomerUser")]
    public async Task<ActionResult> PackagePreview([FromBody] PackageRequest request)
    {
        var customerId = GetCustomerId();
        var result = await _paymentService.GetPackagePreviewAsync(customerId, request.PackageGroupId);
        if (result == null) return NotFound(new { error = "Paket bulunamadi." });
        return Ok(result);
    }

    /// <summary>Paket satin alma checkout formu (Iyzico)</summary>
    [HttpPost("package-checkout")]
    [Authorize(Roles = "CustomerUser")]
    public async Task<ActionResult> PackageCheckout([FromBody] PackageRequest request)
    {
        var customerId = GetCustomerId();
        var buyerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var callbackUrl = $"{GetApiBaseUrl()}/api/payments/iyzico-callback";
        var result = await _paymentService.InitPackageCheckoutAsync(customerId, request.PackageGroupId, callbackUrl, buyerIp);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error });
        return Ok(new { success = true, htmlContent = result.HtmlContent, token = result.Token });
    }

    /// <summary>Abonelik odeme formu (Iyzico Checkout Form)</summary>
    [HttpPost("subscription-checkout")]
    [Authorize(Roles = "CustomerUser")]
    public async Task<ActionResult> SubscriptionCheckout()
    {
        var customerId = GetCustomerId();
        var buyerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var callbackUrl = $"{GetApiBaseUrl()}/api/payments/iyzico-callback";
        var result = await _paymentService.InitSubscriptionCheckoutAsync(customerId, callbackUrl, buyerIp);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error });
        return Ok(new { success = true, htmlContent = result.HtmlContent, token = result.Token });
    }

    /// <summary>Odeme sonucu sorgula (frontend polling icin)</summary>
    [HttpPost("package-result")]
    [Authorize(Roles = "CustomerUser")]
    public async Task<ActionResult> PackageResult([FromBody] PackageResultRequest request)
    {
        if (string.IsNullOrEmpty(request.Token)) return BadRequest(new { success = false, error = "Token eksik." });
        var tx = await _paymentService.GetTransactionByTokenAsync(request.Token);
        if (tx == null) return NotFound(new { success = false, error = "Islem bulunamadi." });
        if (tx.StatusId == PaymentStatuses.Ids.Beklemede)
            return Ok(new { success = false, pending = true, error = "Odeme henuz tamamlanmadi." });
        if (tx.StatusId == PaymentStatuses.Ids.Basarili)
            return Ok(new { success = true });
        return Ok(new { success = false, error = tx.ErrorMessage ?? "Odeme basarisiz." });
    }

    /// <summary>Iyzico checkout form callback (3DS sonrasi)</summary>
    [HttpPost("iyzico-callback")]
    [AllowAnonymous]
    public async Task<ActionResult> IyzicoCallback([FromForm] string? token)
    {
        if (string.IsNullOrEmpty(token)) return BadRequest("Token eksik.");
        var result = await _paymentService.CompleteCheckoutAsync(token);
        var topLevelReturn = await BuildIyzicoTopLevelReturnUrlAsync(token, result.Success, result.Error);
        var html = BuildIyzicoCallbackHtmlPage(result.Success, result.Error, token, topLevelReturn);
        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>
    /// Ust pencere / tam ekran donusu: Iyzico POST cevabini gosteren sayfa, Salon(veya Web) uzerine yonlendirir;
    /// iframe: parent.postMessage. Popup: opener + close, olmazsa ayni URL.
    /// </summary>
    private async Task<string> BuildIyzicoTopLevelReturnUrlAsync(string token, bool paymentSuccess = false, string? error = null)
    {
        var tx = await _paymentService.GetTransactionByTokenAsync(token);
        var salon = (_configuration["Salon:BaseUrl"] ?? "https://sln.corplynk.com").TrimEnd('/');
        var web = (_configuration["WebApp:BaseUrl"] ?? "https://cc.corplynk.com").TrimEnd('/');
        var t = Uri.EscapeDataString(token);
        if (tx == null)
            return $"{salon}/Modules?iyzicoToken={t}";

        if (tx.PaymentTypeId == PaymentTypes.Ids.RandevuOnOdemesi && tx.Notes?.StartsWith("Appointment:") == true)
            return BuildBookingReturnUrl(tx, salon, t, paymentSuccess, error);

        return tx.PaymentTypeId switch
        {
            PaymentTypes.Ids.ModulSatinAlma => $"{salon}/Modules?iyzicoToken={t}",
            PaymentTypes.Ids.PlatformAbonelik => $"{web}/?iyzicoToken={t}",
            _ => $"{salon}/Modules?iyzicoToken={t}"
        };
    }

    private static string BuildBookingReturnUrl(PaymentTransaction tx, string salonBase, string t, bool success, string? error)
    {
        var slug = "";
        var parts = tx.Notes?.Split('|') ?? Array.Empty<string>();
        foreach (var part in parts)
            if (part.StartsWith("Slug:")) slug = part.Replace("Slug:", "");

        if (string.IsNullOrEmpty(slug))
            return $"{salonBase}/Modules?iyzicoToken={t}";

        var url = $"{salonBase}/salon/{slug}/book?iyzicoToken={t}&paid={success.ToString().ToLower()}";
        if (!success && !string.IsNullOrEmpty(error))
            url += $"&payerr={Uri.EscapeDataString(error)}";
        return url;
    }

    private static string BuildIyzicoCallbackHtmlPage(bool success, string? error, string token, string topLevelReturnUrl)
    {
        var js = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var successJson = JsonSerializer.Serialize(success, js);
        var err = error ?? "Odeme basarisiz";
        var errJson = JsonSerializer.Serialize(err, js);
        var returnJson = JsonSerializer.Serialize(topLevelReturnUrl, js);
        return $@"<!DOCTYPE html><html><head><meta charset=""utf-8""><title>Odeme</title></head><body>
<script>
(function() {{
  var success = {successJson};
  var err = {errJson};
  var returnUrl = {returnJson};

  if (window.opener && !window.opener.closed) {{
    try {{
      if (success) window.opener.postMessage({{ type: 'payment-success' }}, '*');
      else window.opener.postMessage({{ type: 'payment-failed', error: err }}, '*');
    }} catch (e) {{}}
    window.close();
    setTimeout(function() {{ if (!document.hidden) window.location.replace(returnUrl); }}, 500);
    return;
  }}
  if (window.parent && window.parent !== window) {{
    if (success) window.parent.postMessage({{ type: 'payment-success' }}, '*');
    else window.parent.postMessage({{ type: 'payment-failed', error: err }}, '*');
    return;
  }}
  window.location.replace(returnUrl);
}})();
</script>
<p style=""font-family:system-ui;padding:1rem"">{(success ? "Odeme isleniyor, yonlendiriliyorsunuz." : "Odeme sonucu isleniyor, yonlendiriliyorsunuz.")}</p>
</body></html>";
    }

    /// <summary>Odeme gecmisi (admin: firma bazli, platform user: kendi odemeleri)</summary>
    [HttpGet("history")]
    public async Task<ActionResult> GetHistory([FromQuery] int? customerId, [FromQuery] int page = 1)
    {
        int? platformUserId = null;
        if (User.IsInRole("PlatformUser"))
            platformUserId = GetPlatformUserId();

        var transactions = await _paymentService.GetTransactionsAsync(customerId, platformUserId, page);
        return Ok(transactions.Select(t => new
        {
            t.Uid,
            t.PaymentTypeId,
            PaymentType = PaymentTypes.GetById(t.PaymentTypeId)?.Description,
            t.Amount,
            t.Currency,
            t.StatusId,
            Status = PaymentStatuses.GetById(t.StatusId)?.Description,
            t.Provider,
            t.CardLastFour,
            t.InstallmentCount,
            t.ModuleId,
            CustomerName = t.Customer?.Name,
            t.CreatedAt,
            t.CompletedAt,
            t.ErrorMessage
        }));
    }

    /// <summary>Platform kullanicisi odeme dekontu (HTML; yazdir veya PDF olarak kaydet)</summary>
    [HttpGet("my-receipt/{uid:guid}")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<IActionResult> DownloadMyReceipt(Guid uid)
    {
        var (bytes, fileName, error) = await _paymentService.GetPlatformUserReceiptHtmlAsync(uid, GetPlatformUserId());
        if (error != null) return NotFound(new { message = error });
        return File(bytes!, "text/html; charset=utf-8", fileName);
    }

    private int GetPlatformUserId()
        => int.Parse(User.FindFirstValue("PlatformUserId") ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirstValue("CustomerId") ?? "0");

    /// <summary>
    /// Callback URL tabanini dondurur. ApiBaseUrl env var set edilmisse onu kullanir;
    /// yoksa Request.Scheme + Request.Host'a duser (dev ortami icin yeterli).
    /// Cloud Run gib proxy arkasinda ApiBaseUrl env var'i https:// ile set edilmeli.
    /// </summary>
    private string GetApiBaseUrl()
    {
        var configured = _configuration["ApiBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');
        return $"{Request.Scheme}://{Request.Host}";
    }
}

public class InvoicePaymentRequest
{
    public int CustomerId { get; set; }
    public int InvoiceId { get; set; }
    public PaymentCardInfo Card { get; set; } = new();
}

public class BillingPaymentRequest
{
    public int BillingPeriodId { get; set; }
    public PaymentCardInfo Card { get; set; } = new();
}

public class ModulePurchaseRequest
{
    public int ModuleId { get; set; }
    public PaymentCardInfo Card { get; set; } = new();
}

public class HavaleRequestDto
{
    public int ModuleId { get; set; }
}

public class HavaleRejectDto
{
    public string? Reason { get; set; }
}

public class MembershipPaymentRequest
{
    public int PlanId { get; set; }
    public int SlnClientId { get; set; }
    public PaymentCardInfo Card { get; set; } = new();
}

public class PackageRequest
{
    public int PackageGroupId { get; set; }
}

public class PackageResultRequest
{
    public string? Token { get; set; }
}

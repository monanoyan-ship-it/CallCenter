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

    /// <summary>
    /// PS.9 — iyzico webhook payload (server-to-server async event).
    /// Iyzico panel: API Anahtarlari → Webhook URL ayarlanmali.
    /// </summary>
    public class IyzicoWebhookPayload
    {
        public string? IyziEventType { get; set; }
        public long? IyziEventTime { get; set; }
        public string? IyziPaymentId { get; set; }
        public string? PaymentConversationId { get; set; }
        public string? Status { get; set; }
        public string? PaymentTransactionId { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? Reason { get; set; }
        /// <summary>Sub-merchant settlement event-lerinde gelen submerchant key</summary>
        public string? SubMerchantKey { get; set; }
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
        var result = await _paymentService.CreateHavaleRequestAsync(
            customerId,
            request.ModuleId,
            request.ReferenceNote,
            request.ReceiptFileName,
            request.ReceiptContentType,
            request.ReceiptBase64);
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
            t.CreatedAt,
            ReferenceNote = TryDecodeNoteValue(TryReadNoteValue(t.Notes, "HavaleRef:")),
            HasReceipt = TryReadNoteValue(t.Notes, "HavaleReceiptPath:") != null,
            ReceiptFileName = TryDecodeNoteValue(TryReadNoteValue(t.Notes, "HavaleReceiptName:"))
        }));
    }

    /// <summary>Yuklenen havale dekontu (Admin veya ilgili salon)</summary>
    [HttpGet("havale-receipt/{uid:guid}")]
    [Authorize(Roles = "CustomerUser,Admin")]
    public async Task<IActionResult> DownloadHavaleReceipt(Guid uid)
    {
        var isAdmin = User.IsInRole("Admin");
        int? customerId = isAdmin ? null : GetCustomerId();
        var (bytes, fileName, contentType, error) = await _paymentService.GetHavaleReceiptFileAsync(uid, customerId, isAdmin);
        if (error != null) return NotFound(new { message = error });
        return File(bytes!, contentType!, fileName);
    }

    /// <summary>Salon musteri uyeligi checkout formu (PlatformUser)</summary>
    [HttpPost("membership")]
    [HttpPost("membership-checkout")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult> PayMembership([FromBody] MembershipPaymentRequest request)
    {
        var platformUserId = GetPlatformUserId();
        var buyerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var callbackUrl = $"{GetPaymentCallbackBaseUrl()}/api/payments/iyzico-callback";
        var result = await _paymentService.InitMembershipCheckoutAsync(
            request.PlanId, request.SlnClientId, platformUserId, callbackUrl, request.Slug, buyerIp);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error, message = result.Error });
        return Ok(new { success = true, htmlContent = result.HtmlContent, token = result.Token });
    }

    /// <summary>Paket pro-rata on izleme (fiyat hesabi)</summary>
    [HttpPost("package-preview")]
    [Authorize(Roles = "CustomerUser")]
    public async Task<ActionResult> PackagePreview([FromBody] PackageRequest request)
    {
        var customerId = GetCustomerId();
        var packageGroupId = ResolvePackageGroupId(request);
        if (!packageGroupId.HasValue) return BadRequest(new { error = "Paket veya modul secimi gecersiz." });

        var result = await _paymentService.GetPackagePreviewAsync(customerId, packageGroupId.Value);
        if (result == null) return NotFound(new { error = "Paket bulunamadi." });
        return Ok(result);
    }

    /// <summary>Paket satin alma checkout formu (Iyzico)</summary>
    [HttpPost("package-checkout")]
    [Authorize(Roles = "CustomerUser")]
    public async Task<ActionResult> PackageCheckout([FromBody] PackageRequest request)
    {
        var customerId = GetCustomerId();
        var packageGroupId = ResolvePackageGroupId(request);
        if (!packageGroupId.HasValue) return BadRequest(new { success = false, error = "Paket veya modul secimi gecersiz." });

        var buyerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var callbackUrl = $"{GetPaymentCallbackBaseUrl()}/api/payments/iyzico-callback";
        var result = await _paymentService.InitPackageCheckoutAsync(customerId, packageGroupId.Value, callbackUrl, buyerIp);
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
        var callbackUrl = $"{GetPaymentCallbackBaseUrl()}/api/payments/iyzico-callback";
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
            return Ok(new
            {
                success = true,
                paymentTypeId = tx.PaymentTypeId,
                message = tx.PaymentTypeId == PaymentTypes.Ids.PlatformAbonelik
                    ? "Abonelik odemeniz basarili. Tahakkuk kapatildi."
                    : "Modulunuz aktif edildi.",
                requiresSessionRefresh = tx.PaymentTypeId == PaymentTypes.Ids.ModulSatinAlma
            });
        if (tx.StatusId == PaymentStatuses.Ids.Iptal)
            return Ok(new { success = false, error = tx.ErrorMessage ?? "Odeme denemesi iptal edildi." });
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
    /// PS.9 — iyzico webhook server-to-server async event handler. Sub-merchant
    /// settlement, refund, balance-funded, marketplace gibi event-leri yakalar
    /// ve ilgili PaymentTransaction.Notes alanina event log yazar. Asil tutar
    /// hesabi yapmaz (settlement raporu iyzico panelden cekilir); bu endpoint
    /// hak edis takibinde audit trail olarak kullanilir.
    ///
    /// Iyzico webhook URL: https://sln.corplynk.com/api/payments/iyzico-webhook
    /// (Merchant panel -> API Anahtarlari -> Webhook URL, Salon public proxy uzerinden)
    /// </summary>
    [HttpPost("iyzico-webhook")]
    [AllowAnonymous]
    public async Task<ActionResult> IyzicoWebhook([FromBody] IyzicoWebhookPayload? payload)
    {
        if (payload == null || string.IsNullOrEmpty(payload.IyziEventType))
            return BadRequest(new { message = "Webhook payload bos veya event type eksik." });

        var (handled, message) = await _paymentService.HandleIyzicoWebhookAsync(payload);
        // Iyzico, 200 OK haricinde retry yapar — handle edilemese bile 200 don
        // (manual review icin loglanir, retry gurultusu olusturmamak icin).
        return Ok(new { received = true, handled, message });
    }

    /// <summary>
    /// Ust pencere / tam ekran donusu: Iyzico POST cevabini gosteren sayfa, Salon(veya Web) uzerine yonlendirir;
    /// iframe: parent.postMessage. Popup: opener + close, olmazsa ayni URL.
    /// </summary>
    private async Task<string> BuildIyzicoTopLevelReturnUrlAsync(string token, bool paymentSuccess = false, string? error = null)
    {
        var tx = await _paymentService.GetTransactionByTokenAsync(token);
        var salon = (_configuration["Salon:BaseUrl"] ?? "https://sln.corplynk.com").TrimEnd('/');
        var t = Uri.EscapeDataString(token);
        if (tx == null)
            return $"{salon}/Modules?iyzicoToken={t}";

        if (tx.PaymentTypeId == PaymentTypes.Ids.RandevuOnOdemesi && tx.Notes?.StartsWith("Appointment:") == true)
            return BuildBookingReturnUrl(tx, salon, t, paymentSuccess, error);
        if (tx.PaymentTypeId == PaymentTypes.Ids.SalonAdisyon && tx.Notes?.StartsWith("PayAppointment:") == true)
            return BuildPlatformPanelReturnUrl(salon, t, paymentSuccess, error);

        return tx.PaymentTypeId switch
        {
            PaymentTypes.Ids.ModulSatinAlma => $"{salon}/Modules?iyzicoToken={t}",
            // Platform tahakkuku Salon yöneticisi KK ile öder; Web köküne göndermek çift kilit / yanlış uygulama yaratıyordu
            PaymentTypes.Ids.PlatformAbonelik => $"{salon}/Modules?iyzicoToken={t}",
            PaymentTypes.Ids.UyelikOdemesi => BuildMembershipReturnUrl(tx, salon, t, paymentSuccess, error),
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

    private static string BuildPlatformPanelReturnUrl(string salonBase, string t, bool success, string? error)
    {
        var url = $"{salonBase}/user/panel?iyzicoToken={t}&paid={success.ToString().ToLower()}";
        if (!success && !string.IsNullOrEmpty(error))
            url += $"&payerr={Uri.EscapeDataString(error)}";
        return url;
    }

    private static string BuildMembershipReturnUrl(PaymentTransaction tx, string salonBase, string t, bool success, string? error)
    {
        var slug = TryReadMembershipSlug(tx.Notes);
        var url = string.IsNullOrWhiteSpace(slug)
            ? $"{salonBase}/user/panel?iyzicoToken={t}&paid={success.ToString().ToLower()}"
            : $"{salonBase}/salon/{Uri.EscapeDataString(slug)}?iyzicoToken={t}&paid={success.ToString().ToLower()}";
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
        int? scopedCustomerId = customerId;
        int? platformUserId = null;

        if (User.IsInRole("PlatformUser"))
        {
            platformUserId = GetPlatformUserId();
            scopedCustomerId = null;
        }
        else if (User.IsInRole("CustomerUser"))
        {
            scopedCustomerId = GetCustomerId();
        }
        else if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var organizationPaymentTypeIds = User.IsInRole("CustomerUser")
            ? new[] { PaymentTypes.Ids.PlatformAbonelik, PaymentTypes.Ids.ModulSatinAlma }
            : null;

        var transactions = await _paymentService.GetTransactionsAsync(
            scopedCustomerId,
            platformUserId,
            page,
            paymentTypeIds: organizationPaymentTypeIds);
        return Ok(transactions.Select(t => new
        {
            t.Uid,
            t.PaymentTypeId,
            PaymentType = PaymentTypes.GetById(t.PaymentTypeId)?.Description,
            t.Amount,
            t.Currency,
            t.PaymentMethodId,
            PaymentMethod = BillingPaymentMethods.GetById(t.PaymentMethodId)?.Description,
            t.StatusId,
            Status = PaymentStatuses.GetById(t.StatusId)?.Description,
            t.Provider,
            t.CardLastFour,
            t.InstallmentCount,
            t.ModuleId,
            PackageGroupId = TryReadPackageGroupId(t.Notes),
            CustomerName = t.Customer?.Name,
            t.CreatedAt,
            t.CompletedAt,
            t.ErrorMessage,
            CanDownloadReceipt = t.StatusId == PaymentStatuses.Ids.Basarili || t.StatusId == PaymentStatuses.Ids.Iade,
            CanDownloadHavaleReceipt = TryReadNoteValue(t.Notes, "HavaleReceiptPath:") != null,
            CanRetry = t.StatusId == PaymentStatuses.Ids.Basarisiz
                && (t.PaymentTypeId == PaymentTypes.Ids.PlatformAbonelik || t.PaymentTypeId == PaymentTypes.Ids.ModulSatinAlma)
        }));
    }

    /// <summary>Admin tam/kismi iade baslatir</summary>
    [HttpPost("{uid:guid}/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Refund(Guid uid, [FromBody] PaymentRefundRequest? request = null)
    {
        var result = await _paymentService.RefundTransactionAsync(uid, request?.Amount, request?.Reason);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(new { transactionId = result.TransactionUid, providerTxId = result.ProviderTransactionId, message = "Iade kaydi olusturuldu." });
    }

    /// <summary>Admin bekleyen odemeyi iptal eder</summary>
    [HttpPost("{uid:guid}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Cancel(Guid uid, [FromBody] PaymentCancelRequest? request = null)
    {
        var result = await _paymentService.CancelTransactionAsync(uid, request?.Reason);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(new { transactionId = result.TransactionUid, message = "Odeme iptal edildi." });
    }

    /// <summary>Support/audit zaman cizelgesi</summary>
    [HttpGet("{uid:guid}/timeline")]
    public async Task<ActionResult> GetTimeline(Guid uid)
    {
        int? customerId = null;
        int? platformUserId = null;
        var allowAny = User.IsInRole("Admin");

        if (User.IsInRole("CustomerUser"))
            customerId = GetCustomerId();
        else if (User.IsInRole("PlatformUser"))
            platformUserId = GetPlatformUserId();
        else if (!allowAny)
            return Forbid();

        var (data, error) = await _paymentService.GetPaymentTimelineAsync(uid, customerId, platformUserId, allowAny);
        if (error != null) return NotFound(new { message = error });
        return Ok(data);
    }

    /// <summary>Provider/DB mutabakat ozet raporu</summary>
    [HttpGet("reconciliation")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetReconciliation([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? provider)
    {
        var data = await _paymentService.GetPaymentReconciliationAsync(from, to, provider);
        return Ok(data);
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

    /// <summary>Salon/management kullanicisi odeme dekontu (HTML; yazdir veya PDF olarak kaydet)</summary>
    [HttpGet("receipt/{uid:guid}")]
    [Authorize(Roles = "CustomerUser,Admin")]
    public async Task<IActionResult> DownloadReceipt(Guid uid)
    {
        var isAdmin = User.IsInRole("Admin");
        var customerId = isAdmin ? 0 : GetCustomerId();
        var (bytes, fileName, error) = await _paymentService.GetCustomerReceiptHtmlAsync(uid, customerId, isAdmin);
        if (error != null) return NotFound(new { message = error });
        return File(bytes!, "text/html; charset=utf-8", fileName);
    }

    /// <summary>PS.4 — Salon iyzico Pazaryeri sub-merchant onboarding (yeni kayit veya mevcut profili guncelle).</summary>
    [HttpPost("sub-merchant")]
    [Authorize(Roles = "CustomerUser")]
    public async Task<ActionResult> OnboardSubMerchant([FromBody] CallCenter.Shared.DTOs.SubMerchantOnboardRequest request)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        if (request == null) return BadRequest(new { message = "İstek gövdesi boş." });

        // Sadece SalonOwner onboard edebilir
        var roleId = User.FindFirstValue("CustomerRoleId");
        if (!int.TryParse(roleId, out var rid) || rid != SalonRoles.Ids.SalonOwner)
            return Forbid();

        var subType = (request.SubMerchantType ?? "PERSONAL").ToUpperInvariant();
        if (subType != "PERSONAL" && subType != "PRIVATE_COMPANY" && subType != "LIMITED_OR_JOINT_STOCK_COMPANY")
            return BadRequest(new { message = "Sub-merchant tipi geçersiz." });
        if (string.IsNullOrWhiteSpace(request.Iban))
            return BadRequest(new { message = "IBAN zorunlu." });
        if (string.IsNullOrWhiteSpace(request.ContactName) || string.IsNullOrWhiteSpace(request.ContactSurname))
            return BadRequest(new { message = "Yetkili ad ve soyad zorunlu." });
        if (subType == "PERSONAL" && string.IsNullOrWhiteSpace(request.IdentityNumber))
            return BadRequest(new { message = "Şahıs için TC kimlik no zorunlu." });
        if (subType != "PERSONAL" && (string.IsNullOrWhiteSpace(request.LegalCompanyTitle) || string.IsNullOrWhiteSpace(request.TaxOffice) || string.IsNullOrWhiteSpace(request.TaxNumber)))
            return BadRequest(new { message = "Şirket için ünvan, vergi dairesi ve vergi numarası zorunlu." });

        // Customer ve SlnSalonProfile bilgilerinden gsm/email/address al
        var customerData = await _paymentService.GetCustomerOnboardingContextAsync(customerId);
        if (customerData == null)
            return NotFound(new { message = "Salon bulunamadı." });

        var iyzReq = new CallCenter.Api.Services.Payment.IyzicoSubMerchantOnboardRequest
        {
            SubMerchantType = subType,
            Name = customerData.Name,
            Email = customerData.Email,
            GsmNumber = customerData.Phone,
            Address = customerData.Address,
            Iban = request.Iban.Trim(),
            ContactName = request.ContactName.Trim(),
            ContactSurname = request.ContactSurname.Trim(),
            IdentityNumber = request.IdentityNumber?.Trim(),
            LegalCompanyTitle = request.LegalCompanyTitle?.Trim(),
            TaxOffice = request.TaxOffice?.Trim(),
            TaxNumber = request.TaxNumber?.Trim(),
            Currency = "TRY",
            SubMerchantExternalId = $"corplynk-salon-{customerId}"
        };

        var (success, error, subMerchantKey) = await _paymentService.OnboardSubMerchantAsync(customerId, iyzReq);
        if (!success) return BadRequest(new { message = error });

        return Ok(new { message = "Pazaryeri kaydı başarılı.", subMerchantKey });
    }

    private int GetPlatformUserId()
        => int.Parse(User.FindFirstValue("PlatformUserId") ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirstValue("CustomerId") ?? "0");

    /// <summary>
    /// Iyzico callback URL tabanini public Salon proxy uzerinden dondurur.
    /// API domain'i public back URL olarak kullanilmaz.
    /// </summary>
    private string GetPaymentCallbackBaseUrl()
    {
        var configured = _configuration["Payment:CallbackBaseUrl"]
            ?? _configuration["Salon:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');
        return "https://sln.corplynk.com";
    }

    private static int? ResolvePackageGroupId(PackageRequest request)
    {
        if (request.PackageGroupId.HasValue)
            return SalonModuleGroups.GetById(request.PackageGroupId.Value) != null ? request.PackageGroupId.Value : null;

        if (request.ModuleId.HasValue)
            return SalonModuleGroups.GetGroupId(request.ModuleId.Value);

        return null;
    }

    private static int? TryReadPackageGroupId(string? notes)
        => TryReadNoteInt(notes, "PackageGroup:");

    private static string? TryReadMembershipSlug(string? notes)
        => TryReadNoteValue(notes, "Slug:");

    private static int? TryReadNoteInt(string? notes, string prefix)
    {
        var value = TryReadNoteValue(notes, prefix);
        return int.TryParse(value, out var id) ? id : null;
    }

    private static string? TryReadNoteValue(string? notes, string prefix)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        foreach (var part in notes.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return part[prefix.Length..];
        }

        return null;
    }

    private static string? TryDecodeNoteValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return Uri.UnescapeDataString(value); }
        catch { return value; }
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
    public string? ReferenceNote { get; set; }
    public string? ReceiptFileName { get; set; }
    public string? ReceiptContentType { get; set; }
    public string? ReceiptBase64 { get; set; }
}

public class HavaleRejectDto
{
    public string? Reason { get; set; }
}

public class PaymentRefundRequest
{
    public decimal? Amount { get; set; }
    public string? Reason { get; set; }
}

public class PaymentCancelRequest
{
    public string? Reason { get; set; }
}

public class MembershipPaymentRequest
{
    public int PlanId { get; set; }
    public int SlnClientId { get; set; }
    public string? Slug { get; set; }
}

public class PackageRequest
{
    public int? PackageGroupId { get; set; }
    public int? ModuleId { get; set; }
}

public class PackageResultRequest
{
    public string? Token { get; set; }
}

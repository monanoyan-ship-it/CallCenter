using System.Security.Claims;
using CallCenter.Api.Services;
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

    public PaymentController(PaymentService paymentService) => _paymentService = paymentService;

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
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/payments/iyzico-callback";
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
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/payments/iyzico-callback";
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
        var html = result.Success
            ? "<html><body><script>window.parent.postMessage({type:'payment-success'},'*');</script><p>Odeme basarili. Sayfa kapanacak...</p></body></html>"
            : $"<html><body><script>window.parent.postMessage({{type:'payment-failed',error:'{result.Error?.Replace("'", "")}'}},'*');</script><p>Odeme basarisiz: {result.Error}</p></body></html>";
        return Content(html, "text/html");
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
            t.CreatedAt,
            t.CompletedAt,
            t.ErrorMessage
        }));
    }

    private int GetPlatformUserId()
        => int.Parse(User.FindFirstValue("PlatformUserId") ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirstValue("CustomerId") ?? "0");
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

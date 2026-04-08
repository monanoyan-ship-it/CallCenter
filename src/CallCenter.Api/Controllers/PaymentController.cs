using System.Security.Claims;
using CallCenter.Api.Services;
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
            t.Amount,
            t.Currency,
            t.StatusId,
            t.Provider,
            t.CardLastFour,
            t.InstallmentCount,
            t.CreatedAt,
            t.CompletedAt,
            t.ErrorMessage
        }));
    }

    private int GetPlatformUserId()
        => int.Parse(User.FindFirstValue("PlatformUserId") ?? "0");
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

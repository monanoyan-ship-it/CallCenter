using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "Admin")]
public class ServiceSubscriptionsController : AuditableControllerBase
{
    private readonly IServiceSubscriptionFactory _factory;

    public ServiceSubscriptionsController(IAuditFactory auditFactory, IServiceSubscriptionFactory factory)
        : base(auditFactory)
    {
        _factory = factory;
    }

    // ═══════════════════════════════════════════════════
    // HİZMET TANIMLARI (TypeDefinition bazlı)
    // ═══════════════════════════════════════════════════

    [HttpGet("services")]
    public ActionResult<List<ServiceTypeDto>> GetAllServices()
    {
        return Ok(_factory.GetAllServices());
    }

    [HttpGet("services/premium")]
    public ActionResult<List<ServiceTypeDto>> GetPremiumServices()
    {
        return Ok(_factory.GetPremiumServices());
    }

    // ═══════════════════════════════════════════════════
    // ABONELİKLER
    // ═══════════════════════════════════════════════════

    [HttpGet("service-subscriptions")]
    public async Task<ActionResult<List<SubscriptionDto>>> GetSubscriptions([FromQuery] int customerId)
    {
        var result = await _factory.GetSubscriptionsByCustomerAsync(customerId);
        return Ok(result);
    }

    [HttpPost("service-subscriptions")]
    public async Task<ActionResult<SubscriptionDto>> CreateSubscription(SubscriptionCreateDto dto)
    {
        var (result, error) = await _factory.CreateSubscriptionAsync(dto);
        if (result == null)
            return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "ServiceSubscription", result.Id.ToString(),
            $"Musteri #{dto.CustomerId} icin '{result.ServiceName}' hizmeti eklendi. Fiyat: {result.MonthlyPrice:N2} TL");

        return Ok(result);
    }

    [HttpPut("service-subscriptions/{uid:guid}")]
    public async Task<ActionResult> UpdateSubscription(Guid uid, SubscriptionUpdateDto dto)
    {
        var (success, error) = await _factory.UpdateSubscriptionAsync(uid, dto);
        if (!success)
            return NotFound(new { message = error });

        await AuditCrudAsync("Update", "ServiceSubscription", uid.ToString(),
            "Abonelik guncellendi.");

        return Ok(new { message = "Guncellendi." });
    }

    [HttpDelete("service-subscriptions/{uid:guid}")]
    public async Task<ActionResult> CancelSubscription(Guid uid)
    {
        var (success, error) = await _factory.CancelSubscriptionAsync(uid);
        if (!success)
            return NotFound(new { message = error });

        await AuditCrudAsync("Cancel", "ServiceSubscription", uid.ToString(),
            "Abonelik iptal edildi.");

        return Ok(new { message = "Abonelik iptal edildi." });
    }

    // ═══════════════════════════════════════════════════
    // TOPLU FİYAT ARTIRMA
    // ═══════════════════════════════════════════════════

    [HttpPost("service-subscriptions/adjust-prices")]
    public async Task<ActionResult<BulkPriceAdjustResultDto>> AdjustPrices(BulkPriceAdjustDto dto)
    {
        var (result, error) = await _factory.AdjustPricesAsync(dto);
        if (result == null)
            return BadRequest(new { message = error });

        await AuditCrudAsync("BulkPriceAdjust", "ServiceSubscription", null,
            $"Toplu fiyat ayarlamasi: {dto.AdjustmentType} {dto.Value}, {result.AffectedCount} abonelik etkilendi. " +
            $"Eski toplam: {result.OldTotalMonthly:N2}, Yeni toplam: {result.NewTotalMonthly:N2}");

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════
    // FATURALANDIRMA
    // ═══════════════════════════════════════════════════

    [HttpPost("service-billing/generate")]
    public async Task<ActionResult> GenerateServiceBilling([FromQuery] int year, [FromQuery] int month)
    {
        var created = await _factory.GenerateServiceBillingAsync(year, month);

        await AuditCrudAsync("Generate", "ServiceBilling", null,
            $"{year}/{month:00} donemi icin {created} hizmet fatura kalemi olusturuldu.");

        return Ok(new { created, message = $"{created} fatura kalemi olusturuldu." });
    }

    [HttpGet("service-billing")]
    public async Task<ActionResult<List<ServiceBillingItemDto>>> GetServiceBilling(
        [FromQuery] int customerId, [FromQuery] int? year, [FromQuery] int? month)
    {
        var result = await _factory.GetServiceBillingByCustomerAsync(customerId, year, month);
        return Ok(result);
    }

    [HttpPut("service-billing/{id:int}/pay")]
    public async Task<ActionResult> MarkBillingPaid(int id)
    {
        var (success, error) = await _factory.MarkServiceBillingPaidAsync(id);
        if (!success)
            return NotFound(new { message = error });

        await AuditCrudAsync("MarkPaid", "ServiceBilling", id.ToString(),
            "Hizmet fatura kalemi odendi olarak isaretlendi.");

        return Ok(new { message = "Odendi olarak isaretlendi." });
    }

    // ═══════════════════════════════════════════════════
    // ÖZET
    // ═══════════════════════════════════════════════════

    [HttpGet("service-subscriptions/summary")]
    public async Task<ActionResult<ServiceSubscriptionSummaryDto>> GetSummary()
    {
        var result = await _factory.GetSubscriptionSummaryAsync();
        return Ok(result);
    }
}

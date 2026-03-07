using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class CustomersController : AuditableControllerBase
{
    private readonly ICustomerFactory _customerFactory;
    private readonly IBillingFactory _billingFactory;

    public CustomersController(IAuditFactory auditFactory, ICustomerFactory customerFactory, IBillingFactory billingFactory) : base(auditFactory)
    {
        _customerFactory = customerFactory;
        _billingFactory = billingFactory;
    }

    /// <summary>Sayfalamali musteri listesi</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        return Ok(await _customerFactory.GetAllAsync(page, pageSize, search));
    }

    /// <summary>Musteri detay (personel listesi dahil)</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(int id)
    {
        var result = await _customerFactory.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Musteri bulunamadi." });
        return Ok(result);
    }

    /// <summary>Yeni musteri olustur (admin kullanici bilgileri form'dan alinir)</summary>
    [HttpPost]
    public async Task<ActionResult> Create(CustomerCreateDto dto)
    {
        var (id, error) = await _customerFactory.CreateAsync(dto);

        if (id == 0)
            return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "Customer", id.ToString(),
            $"Musteri olusturuldu: '{dto.Name}' (admin: {dto.AdminUserName})");

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Musteri guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, CustomerUpdateDto dto)
    {
        var (success, error) = await _customerFactory.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "Customer", id.ToString(),
            $"Musteri guncellendi: ID={id}");

        return NoContent();
    }

    /// <summary>Musteri sil (soft delete)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var (success, error) = await _customerFactory.DeleteAsync(id);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "Customer", id.ToString(),
            $"Musteri silindi (deaktif): ID={id}");

        return NoContent();
    }

    /// <summary>Musteri admin sifresini sifirla — yeni gecici sifre uretir</summary>
    [HttpPost("{id}/reset-admin-password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ResetAdminPassword(int id)
    {
        var (success, tempPassword, error) = await _customerFactory.ResetAdminPasswordAsync(id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("PasswordReset", "Customer", id.ToString(),
            $"Musteri admin sifresi sifirlandi: ID={id}");

        return Ok(new { password = tempPassword });
    }

    // BILLING

    /// <summary>Musteri faturalama donemleri listesi</summary>
    [HttpGet("{id}/billing")]
    public async Task<ActionResult<List<BillingPeriodDto>>> GetBilling(int id)
    {
        return Ok(await _billingFactory.GetByCustomerAsync(id));
    }

    /// <summary>Manuel ilk tahakkuk olustur</summary>
    [HttpPost("{id}/billing/create")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreateBillingPeriod(int id, BillingPeriodCreateDto dto)
    {
        dto.CustomerId = id;
        var (success, error) = await _billingFactory.CreateManualPeriodAsync(dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "BillingPeriod", id.ToString(),
            $"Manuel tahakkuk olusturuldu: CustomerId={id}, Tarih={dto.PeriodStartDate:dd.MM.yyyy}");

        return Ok(new { message = "Tahakkuk olusturuldu." });
    }

    /// <summary>Faturalama donemi guncelle (durum gecisi / not)</summary>
    [HttpPut("billing/{periodId}")]
    public async Task<ActionResult> UpdateBillingPeriod(int periodId, BillingPeriodUpdateDto dto)
    {
        var (success, error) = await _billingFactory.UpdatePeriodAsync(periodId, dto);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "BillingPeriod", periodId.ToString(),
            $"Faturalama donemi guncellendi: ID={periodId}, StatusId={dto.StatusId}, IsPaid={dto.IsPaid}");

        return NoContent();
    }

    /// <summary>Faturalama donemi sil (sadece odenmemis donemler)</summary>
    [HttpDelete("billing/{periodId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteBillingPeriod(int periodId)
    {
        var (success, error) = await _billingFactory.DeletePeriodAsync(periodId);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Delete", "BillingPeriod", periodId.ToString(),
            $"Faturalama donemi silindi: ID={periodId}");

        return NoContent();
    }

    /// <summary>Toplu faturalama donemi olustur (tum aktif musteriler)</summary>
    [HttpPost("billing/generate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GenerateBulkBilling(BulkBillingGenerateDto dto)
    {
        var (created, skipped, skippedNoAnchor, error) = await _billingFactory.GenerateBulkAsync(dto.Year, dto.Month);
        if (error != null) return BadRequest(new { message = error });

        await AuditCrudAsync("BulkGenerate", "BillingPeriod", null,
            $"Toplu faturalama: {dto.Year}/{dto.Month}, olusturulan={created}, atlanan={skipped}, tahakkukyok={skippedNoAnchor}");

        return Ok(new { created, skipped, skippedNoAnchor });
    }

    /// <summary>Muhasebeci raporu: tum musteriler icin faturalama donemleri</summary>
    [HttpGet("~/api/billing/report")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<BillingReportDto>>> GetBillingReport(
        [FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? statusId)
    {
        return Ok(await _billingFactory.GetBillingReportAsync(year, month, statusId));
    }
}

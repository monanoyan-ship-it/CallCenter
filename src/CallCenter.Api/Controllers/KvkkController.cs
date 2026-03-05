using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class KvkkController : AuditableControllerBase
{
    private readonly IKvkkFactory _kvkkFactory;

    public KvkkController(IAuditFactory auditFactory, IKvkkFactory kvkkFactory)
        : base(auditFactory)
    {
        _kvkkFactory = kvkkFactory;
    }

    // ═══════════════════════════════════════════════════════════════
    // CONSENT (RIZA)
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("consents")]
    public async Task<IActionResult> GetConsents([FromQuery] int? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _kvkkFactory.GetConsentsAsync(customerId, page, pageSize);
        return Ok(new { Items = items, TotalCount = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpPost("consents")]
    public async Task<IActionResult> CreateConsent([FromBody] ConsentCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, result) = await _kvkkFactory.CreateConsentAsync(dto);
        if (!success) return BadRequest(new { error = result });

        await AuditCrudAsync("Create", "ConsentRecord", result?.ToString(),
            $"KVKK riza kaydi olusturuldu: {dto.SubjectName}", customerId: dto.CustomerId);

        return Ok(result);
    }

    [HttpPost("consents/{uid}/revoke")]
    public async Task<IActionResult> RevokeConsent(Guid uid, [FromBody] ConsentRevokeDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, error) = await _kvkkFactory.RevokeConsentAsync(uid, dto.RevokedBy);
        if (!success) return BadRequest(new { error });

        await AuditCrudAsync("Revoke", "ConsentRecord", uid.ToString(),
            $"KVKK riza iptal edildi. Iptal eden: {dto.RevokedBy}");

        return Ok(new { message = "Riza basariyla iptal edildi." });
    }

    // ═══════════════════════════════════════════════════════════════
    // DATA SUBJECT REQUEST (İLGİLİ KİŞİ BAŞVURUSU)
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests([FromQuery] int? customerId, [FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _kvkkFactory.GetRequestsAsync(customerId, status, page, pageSize);
        return Ok(new { Items = items, TotalCount = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] DataSubjectRequestCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, result) = await _kvkkFactory.CreateRequestAsync(dto);
        if (!success) return BadRequest(new { error = result });

        await AuditCrudAsync("Create", "DataSubjectRequest", result?.ToString(),
            $"KVKK basvurusu olusturuldu: {dto.RequesterName}", customerId: dto.CustomerId);

        return Ok(result);
    }

    [HttpPut("requests/{uid}")]
    public async Task<IActionResult> UpdateRequest(Guid uid, [FromBody] DataSubjectRequestUpdateDto dto)
    {
        var (success, error) = await _kvkkFactory.UpdateRequestAsync(uid, dto);
        if (!success) return BadRequest(new { error });

        await AuditCrudAsync("Update", "DataSubjectRequest", uid.ToString(),
            $"KVKK basvurusu guncellendi. Durum: {dto.StatusId}");

        return Ok(new { message = "Basvuru guncellendi." });
    }

    [HttpGet("requests/overdue")]
    public async Task<IActionResult> GetOverdueRequests()
    {
        var items = await _kvkkFactory.GetOverdueRequestsAsync();
        return Ok(items);
    }

    // ═══════════════════════════════════════════════════════════════
    // DATA BREACH (VERİ İHLALİ)
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("breaches")]
    public async Task<IActionResult> GetBreaches([FromQuery] int? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _kvkkFactory.GetBreachesAsync(customerId, page, pageSize);
        return Ok(new { Items = items, TotalCount = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpPost("breaches")]
    public async Task<IActionResult> CreateBreach([FromBody] DataBreachCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, result) = await _kvkkFactory.CreateBreachAsync(dto, CurrentUserName, CurrentUserId);
        if (!success) return BadRequest(new { error = result });

        await AuditCrudAsync("Create", "DataBreach", result?.ToString(),
            $"KVKK veri ihlali kaydi olusturuldu: {dto.Title}", customerId: dto.CustomerId);

        return Ok(result);
    }

    [HttpPut("breaches/{uid}")]
    public async Task<IActionResult> UpdateBreach(Guid uid, [FromBody] DataBreachUpdateDto dto)
    {
        var (success, error) = await _kvkkFactory.UpdateBreachAsync(uid, dto);
        if (!success) return BadRequest(new { error });

        await AuditCrudAsync("Update", "DataBreach", uid.ToString(),
            $"KVKK veri ihlali guncellendi. Durum: {dto.StatusId}");

        return Ok(new { message = "Ihlal kaydi guncellendi." });
    }

    // ═══════════════════════════════════════════════════════════════
    // RETENTION POLICY (SAKLAMA POLİTİKASI)
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("retention")]
    public async Task<IActionResult> GetRetentionPolicies([FromQuery] int customerId)
    {
        var items = await _kvkkFactory.GetPoliciesAsync(customerId);
        return Ok(items);
    }

    [HttpPost("retention")]
    public async Task<IActionResult> CreateOrUpdateRetentionPolicy([FromBody] RetentionPolicyCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, result) = await _kvkkFactory.CreateOrUpdatePolicyAsync(dto);
        if (!success) return BadRequest(new { error = result });

        await AuditCrudAsync("CreateOrUpdate", "RetentionPolicy", result?.ToString(),
            $"KVKK saklama politikasi guncellendi. Kategori: {dto.CategoryId}", customerId: dto.CustomerId);

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // DATA DESTRUCTION LOG (VERİ İMHA KAYDI)
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("destruction")]
    public async Task<IActionResult> GetDestructionLogs([FromQuery] int? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _kvkkFactory.GetDestructionLogsAsync(customerId, page, pageSize);
        return Ok(new { Items = items, TotalCount = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpPost("destruction")]
    public async Task<IActionResult> LogDestruction([FromBody] DataDestructionCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userName = CurrentUserName ?? "Bilinmiyor";
        var (success, result) = await _kvkkFactory.LogDestructionAsync(dto, userName, CurrentUserId);
        if (!success) return BadRequest(new { error = result });

        await AuditCrudAsync("Create", "DataDestructionLog", result?.ToString(),
            $"KVKK veri imha kaydi olusturuldu: {dto.DataCategory} ({dto.DestroyedRecordCount} kayit)", customerId: dto.CustomerId);

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // DATA INVENTORY (VERİ ENVANTERİ)
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory([FromQuery] int? customerId)
    {
        var items = await _kvkkFactory.GetInventoryAsync(customerId);
        return Ok(items);
    }

    [HttpPost("inventory")]
    public async Task<IActionResult> CreateOrUpdateInventoryItem([FromBody] DataInventoryCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, result) = await _kvkkFactory.CreateOrUpdateItemAsync(dto);
        if (!success) return BadRequest(new { error = result });

        await AuditCrudAsync("CreateOrUpdate", "DataInventoryItem", result?.ToString(),
            $"KVKK veri envanteri guncellendi: {dto.DataCategory}", customerId: dto.CustomerId);

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // DASHBOARD
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int? customerId)
    {
        var dashboard = await _kvkkFactory.GetDashboardAsync(customerId);
        return Ok(dashboard);
    }
}

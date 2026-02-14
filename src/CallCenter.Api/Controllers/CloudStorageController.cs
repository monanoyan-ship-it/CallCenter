using CallCenter.Api.Services;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class CloudStorageController : AuditableControllerBase
{
    public CloudStorageController(ServiceFactory factory) : base(factory) { }

    // ═══════════════════════════════════════════════════════════════
    // PROVIDER BILGISI
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Desteklenen depolama saglayicilari listesi</summary>
    [HttpGet("providers")]
    public ActionResult<List<StorageProviderInfoDto>> GetProviders()
    {
        var service = Factory.CreateCloudStorageService();
        return Ok(service.GetAvailableProviders());
    }

    // ═══════════════════════════════════════════════════════════════
    // CONFIG CRUD
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Storage config listesi (opsiyonel customerId filtresi)</summary>
    [HttpGet("configs")]
    public async Task<ActionResult<List<StorageConfigListDto>>> GetConfigs([FromQuery] int? customerId)
    {
        var service = Factory.CreateCloudStorageService();
        var configs = await service.GetConfigsAsync(customerId);
        return Ok(configs);
    }

    /// <summary>Storage config detayi (credential'lar maskelenmis)</summary>
    [HttpGet("configs/{id:int}")]
    public async Task<ActionResult<StorageConfigDetailDto>> GetConfig(int id)
    {
        var service = Factory.CreateCloudStorageService();
        var config = await service.GetConfigByIdAsync(id);
        if (config == null) return NotFound("Config bulunamadi");
        return Ok(config);
    }

    /// <summary>Yeni storage config olustur</summary>
    [HttpPost("configs")]
    public async Task<ActionResult> CreateConfig(StorageConfigCreateDto dto)
    {
        var service = Factory.CreateCloudStorageService();
        var (success, id, error) = await service.CreateConfigAsync(dto);

        if (!success) return BadRequest(error);

        await AuditCrudAsync("Create", "CloudStorage", id?.ToString(),
            $"Musteri {dto.CustomerId} icin {StorageProviders.GetById(dto.ProviderTypeId)?.SystemName} storage config olusturuldu");

        return CreatedAtAction(nameof(GetConfig), new { id }, new { id });
    }

    /// <summary>Storage config guncelle</summary>
    [HttpPut("configs/{id:int}")]
    public async Task<ActionResult> UpdateConfig(int id, StorageConfigUpdateDto dto)
    {
        var service = Factory.CreateCloudStorageService();
        var (success, error) = await service.UpdateConfigAsync(id, dto);

        if (!success) return BadRequest(error);

        await AuditCrudAsync("Update", "CloudStorage", id.ToString(), $"Storage config {id} guncellendi");

        return NoContent();
    }

    /// <summary>Storage config sil</summary>
    [HttpDelete("configs/{id:int}")]
    public async Task<ActionResult> DeleteConfig(int id)
    {
        var service = Factory.CreateCloudStorageService();
        var (success, error) = await service.DeleteConfigAsync(id);

        if (!success) return BadRequest(error);

        await AuditCrudAsync("Delete", "CloudStorage", id.ToString(), $"Storage config {id} silindi");

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════
    // BAGLANTI TESTI
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Belirli bir config'in baglantisini test et</summary>
    [HttpPost("configs/{id:int}/test")]
    public async Task<ActionResult<StorageTestResultDto>> TestConnection(int id, CancellationToken ct)
    {
        var service = Factory.CreateCloudStorageService();
        var result = await service.TestConnectionAsync(id, ct);
        return Ok(result);
    }
}

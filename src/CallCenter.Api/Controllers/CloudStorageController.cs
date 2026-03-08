using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "CustomerUser")]
public class CloudStorageController : AuditableControllerBase
{
    private readonly ICloudStorageFactory _cloudStorageFactory;

    public CloudStorageController(IAuditFactory auditFactory, ICloudStorageFactory cloudStorageFactory) : base(auditFactory)
    {
        _cloudStorageFactory = cloudStorageFactory;
    }

    [HttpGet("providers")]
    public ActionResult<List<StorageProviderInfoDto>> GetProviders()
    {
        return Ok(_cloudStorageFactory.GetAvailableProviders());
    }

    [HttpGet("configs")]
    public async Task<ActionResult<List<StorageConfigListDto>>> GetConfigs([FromQuery] int? customerId)
    {
        // CustomerUser sadece kendi firmasini gorebilir
        var cid = CurrentCustomerId;
        if (cid != null)
            customerId = cid.Value;

        return Ok(await _cloudStorageFactory.GetConfigsAsync(customerId));
    }

    [HttpGet("configs/{id:int}")]
    public async Task<ActionResult<StorageConfigDetailDto>> GetConfig(int id)
    {
        var config = await _cloudStorageFactory.GetConfigByIdAsync(id);
        if (config == null) return NotFound("Config bulunamadi");

        // CustomerUser baska firmanin config'ini goremez
        if (CurrentCustomerId != null && config.CustomerId != CurrentCustomerId.Value)
            return Forbid();

        return Ok(config);
    }

    [HttpPost("configs")]
    public async Task<ActionResult> CreateConfig(StorageConfigCreateDto dto)
    {
        // CustomerUser sadece kendi firmasi icin olusturabilir
        if (CurrentCustomerId != null)
            dto.CustomerId = CurrentCustomerId.Value;

        var (success, id, error) = await _cloudStorageFactory.CreateConfigAsync(dto);
        if (!success) return BadRequest(error);

        await AuditCrudAsync("Create", "CloudStorage", id?.ToString(),
            $"Musteri {dto.CustomerId} icin {StorageProviders.GetById(dto.ProviderTypeId)?.SystemName} storage config olusturuldu");

        return CreatedAtAction(nameof(GetConfig), new { id }, new { id });
    }

    [HttpPut("configs/{id:int}")]
    public async Task<ActionResult> UpdateConfig(int id, StorageConfigUpdateDto dto)
    {
        // CustomerUser baska firmanin config'ini guncelleyemez
        if (CurrentCustomerId != null)
        {
            var existing = await _cloudStorageFactory.GetConfigByIdAsync(id);
            if (existing == null || existing.CustomerId != CurrentCustomerId.Value)
                return Forbid();
        }

        var (success, error) = await _cloudStorageFactory.UpdateConfigAsync(id, dto);
        if (!success) return BadRequest(error);

        await AuditCrudAsync("Update", "CloudStorage", id.ToString(), $"Storage config {id} guncellendi");

        return NoContent();
    }

    [HttpDelete("configs/{id:int}")]
    public async Task<ActionResult> DeleteConfig(int id)
    {
        // CustomerUser baska firmanin config'ini silemez
        if (CurrentCustomerId != null)
        {
            var existing = await _cloudStorageFactory.GetConfigByIdAsync(id);
            if (existing == null || existing.CustomerId != CurrentCustomerId.Value)
                return Forbid();
        }

        var (success, error) = await _cloudStorageFactory.DeleteConfigAsync(id);
        if (!success) return BadRequest(error);

        await AuditCrudAsync("Delete", "CloudStorage", id.ToString(), $"Storage config {id} silindi");

        return NoContent();
    }

    [HttpPost("configs/{id:int}/test")]
    public async Task<ActionResult<StorageTestResultDto>> TestConnection(int id, CancellationToken ct)
    {
        // CustomerUser baska firmanin config'ini test edemez
        if (CurrentCustomerId != null)
        {
            var existing = await _cloudStorageFactory.GetConfigByIdAsync(id);
            if (existing == null || existing.CustomerId != CurrentCustomerId.Value)
                return Forbid();
        }

        return Ok(await _cloudStorageFactory.TestConnectionAsync(id, ct));
    }
}

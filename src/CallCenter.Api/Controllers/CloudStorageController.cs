using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Services.CloudStorage;
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
    private readonly OneDriveOAuthService _oneDriveOAuth;

    public CloudStorageController(IAuditFactory auditFactory, ICloudStorageFactory cloudStorageFactory, OneDriveOAuthService oneDriveOAuth) : base(auditFactory)
    {
        _cloudStorageFactory = cloudStorageFactory;
        _oneDriveOAuth = oneDriveOAuth;
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

    // ─── OneDrive OAuth2 Flow ───

    /// <summary>OneDrive OAuth2 baslatma URL'i doner</summary>
    [HttpGet("onedrive/auth-url")]
    public ActionResult<OneDriveAuthUrlDto> GetOneDriveAuthUrl([FromQuery] string? tenantId)
    {
        if (!_oneDriveOAuth.IsConfigured)
            return BadRequest(new { error = "OneDrive OAuth yapilandirilmamis. appsettings'te OneDrive:ClientId/ClientSecret ayarlayin." });

        var state = Guid.NewGuid().ToString("N");
        var url = _oneDriveOAuth.GetAuthorizationUrl(tenantId, state);
        return Ok(new OneDriveAuthUrlDto { AuthUrl = url, State = state });
    }

    /// <summary>Authorization code ile token exchange + drive kesfet</summary>
    [HttpPost("onedrive/exchange-code")]
    public async Task<ActionResult<OneDriveAuthResultDto>> ExchangeOneDriveCode([FromBody] OneDriveExchangeCodeDto dto)
    {
        if (!_oneDriveOAuth.IsConfigured)
            return BadRequest(new OneDriveAuthResultDto { Success = false, Error = "OneDrive OAuth yapilandirilmamis" });

        var token = await _oneDriveOAuth.ExchangeCodeAsync(dto.Code, dto.TenantId);
        if (token == null)
            return Ok(new OneDriveAuthResultDto { Success = false, Error = "Microsoft'tan token alinamadi. Kod gecersiz veya suresi dolmus olabilir." });

        var drives = await _oneDriveOAuth.GetDrivesAsync(token.AccessToken);

        return Ok(new OneDriveAuthResultDto
        {
            Success = true,
            RefreshToken = token.RefreshToken,
            TenantId = dto.TenantId,
            Drives = drives.Select(d => new OneDriveDriveDto
            {
                DriveId = d.DriveId,
                Name = d.Name,
                DriveType = d.DriveType,
                OwnerName = d.OwnerName,
                TotalSpace = d.TotalSpace,
                UsedSpace = d.UsedSpace
            }).ToList()
        });
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

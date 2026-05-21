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
    private readonly GoogleDriveOAuthService _googleDriveOAuth;
    private readonly YandexOAuthService _yandexOAuth;
    private readonly string _webAppBaseUrl;

    public CloudStorageController(
        IAuditFactory auditFactory,
        ICloudStorageFactory cloudStorageFactory,
        OneDriveOAuthService oneDriveOAuth,
        GoogleDriveOAuthService googleDriveOAuth,
        YandexOAuthService yandexOAuth,
        IConfiguration config) : base(auditFactory)
    {
        _cloudStorageFactory = cloudStorageFactory;
        _oneDriveOAuth = oneDriveOAuth;
        _googleDriveOAuth = googleDriveOAuth;
        _yandexOAuth = yandexOAuth;
        _webAppBaseUrl = config["WebApp:BaseUrl"] ?? "https://cc.corplynk.com";
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

    /// <summary>Microsoft OAuth2 redirect callback — Blazor callback sayfasina yonlendirir</summary>
    [HttpGet("onedrive/callback")]
    [AllowAnonymous]
    public IActionResult OneDriveCallback([FromQuery] string? code, [FromQuery] string? error, [FromQuery] string? error_description)
    {
        if (!string.IsNullOrEmpty(code))
            return Redirect($"{_webAppBaseUrl}/oauth/callback?provider=onedrive&code={Uri.EscapeDataString(code)}");
        return Redirect($"{_webAppBaseUrl}/oauth/callback?provider=onedrive&error={Uri.EscapeDataString(error_description ?? error ?? "Bilinmeyen hata")}");
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

    // ─── Google Drive OAuth2 Flow ───

    /// <summary>Google Drive OAuth2 baslatma URL'i doner</summary>
    [HttpGet("googledrive/auth-url")]
    public ActionResult<GoogleDriveAuthUrlDto> GetGoogleDriveAuthUrl()
    {
        if (!_googleDriveOAuth.IsConfigured)
            return BadRequest(new { error = "Google Drive OAuth yapilandirilmamis. appsettings'te GoogleDrive:ClientId/ClientSecret ayarlayin." });

        var state = $"googledrive-{Guid.NewGuid():N}";
        var url = _googleDriveOAuth.GetAuthorizationUrl(state);
        return Ok(new GoogleDriveAuthUrlDto { AuthUrl = url, State = state });
    }

    /// <summary>Google OAuth2 redirect callback — Blazor callback sayfasina yonlendirir</summary>
    [HttpGet("googledrive/callback")]
    [AllowAnonymous]
    public IActionResult GoogleDriveCallback([FromQuery] string? code, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(code))
            return Redirect($"{_webAppBaseUrl}/oauth/callback?provider=googledrive&code={Uri.EscapeDataString(code)}");
        return Redirect($"{_webAppBaseUrl}/oauth/callback?provider=googledrive&error={Uri.EscapeDataString(error ?? "Bilinmeyen hata")}");
    }

    /// <summary>Google Drive authorization code ile token exchange + kullanici bilgisi</summary>
    [HttpPost("googledrive/exchange-code")]
    public async Task<ActionResult<GoogleDriveAuthResultDto>> ExchangeGoogleDriveCode([FromBody] GoogleDriveExchangeCodeDto dto)
    {
        if (!_googleDriveOAuth.IsConfigured)
            return BadRequest(new GoogleDriveAuthResultDto { Success = false, Error = "Google Drive OAuth yapilandirilmamis" });

        var token = await _googleDriveOAuth.ExchangeCodeAsync(dto.Code);
        if (token == null)
            return Ok(new GoogleDriveAuthResultDto { Success = false, Error = "Google'dan token alinamadi. Kod gecersiz veya suresi dolmus olabilir." });

        var userInfo = await _googleDriveOAuth.GetUserInfoAsync(token.AccessToken);
        var folders = await _googleDriveOAuth.GetRootFoldersAsync(token.AccessToken);

        return Ok(new GoogleDriveAuthResultDto
        {
            Success = true,
            RefreshToken = token.RefreshToken,
            Email = userInfo?.Email,
            DisplayName = userInfo?.DisplayName,
            Folders = folders.Select(f => new GoogleDriveFolderDto
            {
                Id = f.Id,
                Name = f.Name
            }).ToList()
        });
    }

    // ─── Yandex OAuth2 Flow ───

    /// <summary>Yandex OAuth2 baslatma URL'i doner</summary>
    [HttpGet("yandex/auth-url")]
    public ActionResult<YandexAuthUrlDto> GetYandexAuthUrl()
    {
        if (!_yandexOAuth.IsConfigured)
            return BadRequest(new { error = "Yandex OAuth yapilandirilmamis. appsettings'te Yandex:ClientId/ClientSecret ayarlayin." });

        var state = Guid.NewGuid().ToString("N");
        var url = _yandexOAuth.GetAuthorizationUrl(state);
        return Ok(new YandexAuthUrlDto { AuthUrl = url, State = state });
    }

    /// <summary>Yandex OAuth2 redirect callback — Blazor callback sayfasina yonlendirir</summary>
    [HttpGet("yandex/callback")]
    [AllowAnonymous]
    public IActionResult YandexCallback([FromQuery] string? code, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(code))
            return Redirect($"{_webAppBaseUrl}/oauth/callback?provider=yandex&code={Uri.EscapeDataString(code)}");
        return Redirect($"{_webAppBaseUrl}/oauth/callback?provider=yandex&error={Uri.EscapeDataString(error ?? "Bilinmeyen hata")}");
    }

    /// <summary>Yandex authorization code ile token exchange + kullanici bilgisi</summary>
    [HttpPost("yandex/exchange-code")]
    public async Task<ActionResult<YandexAuthResultDto>> ExchangeYandexCode([FromBody] YandexExchangeCodeDto dto)
    {
        if (!_yandexOAuth.IsConfigured)
            return BadRequest(new YandexAuthResultDto { Success = false, Error = "Yandex OAuth yapilandirilmamis" });

        var token = await _yandexOAuth.ExchangeCodeAsync(dto.Code);
        if (token == null)
            return Ok(new YandexAuthResultDto { Success = false, Error = "Yandex'ten token alinamadi. Kod gecersiz veya suresi dolmus olabilir." });

        var userInfo = await _yandexOAuth.GetUserInfoAsync(token.AccessToken);

        return Ok(new YandexAuthResultDto
        {
            Success = true,
            OAuthToken = token.AccessToken,
            Login = userInfo?.Login,
            TotalSpace = userInfo?.TotalSpace ?? 0,
            UsedSpace = userInfo?.UsedSpace ?? 0
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

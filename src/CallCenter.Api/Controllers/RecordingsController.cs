using System.Security.Claims;
using System.Text.Json;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

/// <summary>
/// Ses kayitlari: bulut config dagitimi, indirme URL'leri, kayit dinleme (playback).
/// Upload islemi Windows app tarafindan dogrudan bulut'a yapilir.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecordingsController : AuditableControllerBase
{
    private readonly ICloudStorageFactory _cloudStorageFactory;
    private readonly IRecordingPlaybackFactory _playbackFactory;
    private readonly ISettingEntityService _settingEs;
    private readonly ICustomerEntityService _customerEs;
    private readonly AesEncryptionService _aes;
    private readonly Infrastructure.IUnitOfWork _uow;
    private readonly ILogger<RecordingsController> _logger;

    public RecordingsController(
        IAuditFactory auditFactory,
        ICloudStorageFactory cloudStorageFactory,
        IRecordingPlaybackFactory playbackFactory,
        ISettingEntityService settingEs,
        ICustomerEntityService customerEs,
        AesEncryptionService aes,
        Infrastructure.IUnitOfWork uow,
        ILogger<RecordingsController> logger) : base(auditFactory)
    {
        _cloudStorageFactory = cloudStorageFactory;
        _playbackFactory = playbackFactory;
        _settingEs = settingEs;
        _customerEs = customerEs;
        _aes = aes;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>
    /// Musterinin default cloud storage config'ini decrypted dondur.
    /// Windows app bu bilgiyle direkt bulut'a yukler (API proxy yok).
    /// </summary>
    [HttpGet("cloud-config")]
    public async Task<ActionResult<CloudConfigForClientDto>> GetCloudConfig()
    {
        var customerId = CurrentCustomerId;
        if (customerId == null)
            return Unauthorized("CustomerId bulunamadi");

        var config = await _cloudStorageFactory.GetConfigForClientAsync(customerId.Value);

        if (config == null)
            return NotFound("Cloud storage yapilandirilmamis");

        return Ok(config);
    }

    /// <summary>
    /// Ses kaydinin gecici indirme URL'ini olustur (30 dakika gecerli).
    /// </summary>
    [HttpGet("{callUid:guid}/url")]
    public async Task<ActionResult<RecordingDownloadUrlDto>> GetDownloadUrl(
        Guid callUid, CancellationToken ct)
    {
        var customerId = CurrentCustomerId;
        if (customerId == null)
            return Unauthorized("CustomerId bulunamadi");

        var result = await _cloudStorageFactory.GetCallRecordingUrlAsync(customerId.Value, callUid, ct);

        if (result == null)
            return NotFound("Ses kaydi bulunamadi veya bulut'a yuklenmemis");

        return Ok(result);
    }

    /// <summary>
    /// Platform (sistem geneli) cloud storage config'ini dondur.
    /// SystemSettings'ten okur, credentials AES decrypt eder.
    /// </summary>
    [HttpGet("platform-config")]
    public async Task<ActionResult<CloudConfigForClientDto>> GetPlatformConfig()
    {
        var config = await GetPlatformConfigInternalAsync();
        if (config == null)
            return NotFound("Platform depolamasi yapilandirilmamis veya aktif degil");

        return Ok(config);
    }

    /// <summary>
    /// Windows app icin cift upload hedeflerini dondur.
    /// Platform: config varsa aktif, yoksa pasif (SaveRecordingToPlatform flag'i yalnizca override).
    /// Musteri: aktif storage config varsa otomatik aktif (SaveRecordingToOwnStorage flag'i yalnizca override).
    /// </summary>
    [HttpGet("upload-targets")]
    public async Task<ActionResult<RecordingUploadTargetsDto>> GetUploadTargets()
    {
        var customerId = CurrentCustomerId;
        if (customerId == null)
            return Unauthorized("CustomerId bulunamadi");

        var customer = await _customerEs.GetByIdAsync(customerId.Value);
        if (customer == null)
            return NotFound("Musteri bulunamadi");

        var result = new RecordingUploadTargetsDto
        {
            AutoRecordCalls = customer.AutoRecordCalls
        };

        // Platform deposu: config varsa aktif
        if (customer.SaveRecordingToPlatform)
        {
            result.PlatformConfig = await GetPlatformConfigInternalAsync();
            result.UploadToPlatform = result.PlatformConfig != null;
        }

        // Musteri deposu: aktif config varsa otomatik aktif
        var customerConfig = await _cloudStorageFactory.GetConfigForClientAsync(customerId.Value);
        if (customerConfig != null)
        {
            result.UploadToCustomerStorage = true;
            result.CustomerConfig = customerConfig;
        }

        return Ok(result);
    }

    /// <summary>
    /// Musteri admini otomatik kayit ayarini toggle eder.
    /// </summary>
    [HttpPut("auto-record")]
    public async Task<IActionResult> ToggleAutoRecord([FromBody] ToggleAutoRecordRequest request)
    {
        var customerId = CurrentCustomerId;
        if (customerId == null) return Unauthorized();

        // Sadece CustomerAdmin yapabilir
        var isAdmin = User.FindFirstValue("IsCustomerAdmin") == "true";
        if (!isAdmin) return Forbid();

        var customer = await _customerEs.GetByIdAsync(customerId.Value);
        if (customer == null) return NotFound();

        customer.AutoRecordCalls = request.Enabled;
        await _uow.SaveChangesAsync();

        return Ok(new { customer.AutoRecordCalls });
    }

    private async Task<CloudConfigForClientDto?> GetPlatformConfigInternalAsync()
    {
        var settings = await _settingEs.GetAllQueryable()
            .Where(s => s.Key.StartsWith("storage.platform_"))
            .ToListAsync();

        var enabled = settings.FirstOrDefault(s => s.Key == "storage.platform_enabled")?.Value;
        if (enabled != "true") return null;

        var providerTypeIdStr = settings.FirstOrDefault(s => s.Key == "storage.platform_provider_type_id")?.Value;
        if (!int.TryParse(providerTypeIdStr, out var providerTypeId) || providerTypeId == 0) return null;

        var encryptedCredentials = settings.FirstOrDefault(s => s.Key == "storage.platform_credentials")?.Value;
        var basePath = settings.FirstOrDefault(s => s.Key == "storage.platform_base_path")?.Value;

        var credentials = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(encryptedCredentials))
        {
            var decrypted = _aes.Decrypt(encryptedCredentials);
            if (!string.IsNullOrEmpty(decrypted))
            {
                try
                {
                    credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted) ?? new();
                }
                catch { /* gecersiz JSON — bos credentials */ }
            }
        }

        return new CloudConfigForClientDto
        {
            ProviderTypeId = providerTypeId,
            BasePath = basePath,
            Credentials = credentials
        };
    }

    /// <summary>
    /// Musterinin aktif bulut depolama config'i var mi?
    /// </summary>
    [HttpGet("cloud-enabled")]
    public async Task<ActionResult<bool>> IsCloudEnabled()
    {
        var customerId = CurrentCustomerId;
        if (customerId == null)
            return Unauthorized("CustomerId bulunamadi");

        var enabled = await _cloudStorageFactory.HasActiveConfigAsync(customerId.Value);
        return Ok(enabled);
    }

    // ═══════════════════════════════════════════════════════════════
    // PLAYBACK ENDPOINTS (Faz 12)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Kayit bilgisi + yetki kontrolu.
    /// Admin: tum kayitlar, CustomerAdmin: kendi sirketi, Supervisor: ekibi, Agent: erisim YOK.
    /// </summary>
    [HttpGet("{callUid:guid}/info")]
    public async Task<ActionResult<RecordingInfoDto>> GetRecordingInfo(Guid callUid)
    {
        var user = BuildCurrentUser();
        if (user == null) return Unauthorized();

        var info = await _playbackFactory.GetRecordingInfoAsync(callUid, user);
        if (info == null) return Forbid();

        return Ok(info);
    }

    /// <summary>
    /// Ses kaydini stream olarak dondur (decrypt edilmis).
    /// Yetki kontrolu + RecordingAccessLog kaydi olusturulur.
    /// </summary>
    [HttpGet("{callUid:guid}/stream")]
    public async Task<IActionResult> StreamRecording(Guid callUid)
    {
        var user = BuildCurrentUser();
        if (user == null)
        {
            _logger.LogWarning("[StreamRecording] BuildCurrentUser null dondu - Unauthorized");
            return Unauthorized();
        }

        _logger.LogInformation("[StreamRecording] Istek: CallUid={CallUid}, User={UserName}, Role={Role}, IsCustomerAdmin={IsCA}",
            callUid, user.UserName, User.FindFirstValue(System.Security.Claims.ClaimTypes.Role), user.IsCustomerAdmin);

        var result = await _playbackFactory.StreamRecordingAsync(callUid, user, ClientIp, ClientUserAgent);
        if (result == null)
        {
            _logger.LogWarning("[StreamRecording] PlaybackFactory null dondu -> 403 Forbid: CallUid={CallUid}", callUid);
            return Forbid();
        }

        var (audioStream, contentType) = result.Value;
        if (audioStream == null)
        {
            _logger.LogWarning("[StreamRecording] Stream null -> 404: CallUid={CallUid}, Reason={Reason}", callUid, contentType);
            return NotFound(new { error = contentType });
        }

        _logger.LogInformation("[StreamRecording] Stream basarili: CallUid={CallUid}, ContentType={CT}", callUid, contentType);
        return File(audioStream, contentType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Dinleme tamamlandi logu (client tarafindan cagirilir).
    /// </summary>
    [HttpPost("{callUid:guid}/stream-ended")]
    public async Task<IActionResult> StreamEnded(Guid callUid)
    {
        var user = BuildCurrentUser();
        if (user == null) return Unauthorized();

        await _playbackFactory.LogStreamEndedAsync(callUid, user, ClientIp, ClientUserAgent);
        return Ok();
    }

    private CurrentUserInfo? BuildCurrentUser()
    {
        if (CurrentUserId == null) return null;

        var roleStr = User.FindFirstValue(ClaimTypes.Role);
        var personnelIdStr = User.FindFirstValue("PersonnelId");

        return new CurrentUserInfo
        {
            UserId = CurrentUserId.Value,
            UserName = CurrentUserName ?? "",
            CustomerId = CurrentCustomerId,
            IsAdmin = roleStr == "Admin",
            IsCustomerAdmin = User.FindFirstValue("IsCustomerAdmin") == "true",
            IsSupervisor = roleStr == "Supervisor",
            PersonnelId = personnelIdStr != null && int.TryParse(personnelIdStr, out var pid) ? pid : null
        };
    }
}

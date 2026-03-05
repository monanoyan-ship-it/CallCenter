using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public RecordingsController(
        IAuditFactory auditFactory,
        ICloudStorageFactory cloudStorageFactory,
        IRecordingPlaybackFactory playbackFactory) : base(auditFactory)
    {
        _cloudStorageFactory = cloudStorageFactory;
        _playbackFactory = playbackFactory;
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
        if (user == null) return Unauthorized();

        var result = await _playbackFactory.StreamRecordingAsync(callUid, user, ClientIp, ClientUserAgent);
        if (result == null) return Forbid();

        var (audioStream, contentType) = result.Value;
        if (audioStream == null) return NotFound("Ses kaydi bulunamadi");

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

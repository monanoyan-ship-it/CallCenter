using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

/// <summary>
/// Mobil push notification token kayit/silme endpoint'leri.
/// FCM (Android/Web) ve APNs (iOS) token'lari burada toplanir.
/// </summary>
[ApiController]
[Route("api/platform/push-token")]
[Authorize(Roles = "PlatformUser")]
public class PlatformPushTokenController : ControllerBase
{
    private readonly IPlatformPushTokenFactory _factory;

    public PlatformPushTokenController(IPlatformPushTokenFactory factory) => _factory = factory;

    /// <summary>Yeni token kaydet veya mevcut tokenin LastUsedAt'ini guncelle.</summary>
    [HttpPost]
    public async Task<ActionResult> Register([FromBody] PlatformPushTokenRequest request)
    {
        if (request == null) return BadRequest(new { message = "İstek gövdesi boş." });
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Token zorunlu." });

        var platform = (request.Platform ?? "").ToLowerInvariant().Trim();
        if (platform != "ios" && platform != "android" && platform != "web")
            return BadRequest(new { message = "Platform 'ios', 'android' veya 'web' olmalı." });

        var platformUserId = GetPlatformUserId();
        if (platformUserId == 0) return Unauthorized();

        var (success, error) = await _factory.RegisterAsync(platformUserId, request);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Token kaydedildi." });
    }

    /// <summary>Belirtilen token'i pasiflestir (logout veya cihaz cikisi).</summary>
    [HttpDelete]
    public async Task<ActionResult> Unregister([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "Token zorunlu." });

        var platformUserId = GetPlatformUserId();
        if (platformUserId == 0) return Unauthorized();

        var (success, error) = await _factory.UnregisterAsync(platformUserId, token);
        if (!success && error == "Token bulunamadi.") return NotFound();
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    /// <summary>Kullanicinin aktif token'larini listele.</summary>
    [HttpGet]
    public async Task<ActionResult<List<PlatformPushTokenDto>>> List()
    {
        var platformUserId = GetPlatformUserId();
        if (platformUserId == 0) return Unauthorized();

        return Ok(await _factory.ListAsync(platformUserId));
    }

    private int GetPlatformUserId()
        => int.Parse(User.FindFirst("PlatformUserId")?.Value ?? "0");
}

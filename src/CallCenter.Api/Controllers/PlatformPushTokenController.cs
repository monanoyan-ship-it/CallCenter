using System.Security.Claims;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    private readonly AppDbContext _db;

    public PlatformPushTokenController(AppDbContext db) => _db = db;

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

        var now = DateTime.UtcNow;

        // Ayni token mevcutsa update (LastUsedAt + IsActive=true)
        var existing = await _db.PlatformPushTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token);

        if (existing != null)
        {
            // Token baska bir kullaniciya bagliysa devret (cihaz farkli kisi tarafindan kullaniliyor olabilir)
            existing.PlatformUserId = platformUserId;
            existing.Platform = platform;
            existing.DeviceId = request.DeviceId;
            existing.IsActive = true;
            existing.LastUsedAt = now;
            existing.UpdatedAt = now;
        }
        else
        {
            // Ayni cihaza eski tokenlari pasiflestir (varsa)
            if (!string.IsNullOrEmpty(request.DeviceId))
            {
                var oldDeviceTokens = await _db.PlatformPushTokens
                    .Where(t => t.PlatformUserId == platformUserId && t.DeviceId == request.DeviceId && t.IsActive)
                    .ToListAsync();
                foreach (var old in oldDeviceTokens)
                {
                    old.IsActive = false;
                    old.UpdatedAt = now;
                }
            }

            _db.PlatformPushTokens.Add(new PlatformPushToken
            {
                PlatformUserId = platformUserId,
                Token = request.Token,
                Platform = platform,
                DeviceId = request.DeviceId,
                IsActive = true,
                CreatedAt = now,
                LastUsedAt = now
            });
        }

        await _db.SaveChangesAsync();
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

        var entry = await _db.PlatformPushTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.PlatformUserId == platformUserId);
        if (entry == null) return NotFound();

        entry.IsActive = false;
        entry.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Kullanicinin aktif token'larini listele.</summary>
    [HttpGet]
    public async Task<ActionResult<List<PlatformPushTokenDto>>> List()
    {
        var platformUserId = GetPlatformUserId();
        if (platformUserId == 0) return Unauthorized();

        var list = await _db.PlatformPushTokens
            .Where(t => t.PlatformUserId == platformUserId && t.IsActive)
            .OrderByDescending(t => t.LastUsedAt)
            .Select(t => new PlatformPushTokenDto
            {
                Id = t.Id,
                Platform = t.Platform,
                DeviceId = t.DeviceId,
                CreatedAt = t.CreatedAt,
                LastUsedAt = t.LastUsedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    private int GetPlatformUserId()
        => int.Parse(User.FindFirst("PlatformUserId")?.Value ?? "0");
}

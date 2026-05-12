using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class PlatformPushTokenFactory : IPlatformPushTokenFactory
{
    private readonly IPlatformPushTokenEntityService _pushTokens;
    private readonly IUnitOfWork _uow;

    public PlatformPushTokenFactory(
        IPlatformPushTokenEntityService pushTokens,
        IUnitOfWork uow)
    {
        _pushTokens = pushTokens;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(int platformUserId, PlatformPushTokenRequest request)
    {
        if (request == null) return (false, "Istek govdesi bos.");
        if (string.IsNullOrWhiteSpace(request.Token)) return (false, "Token zorunlu.");

        var platform = (request.Platform ?? string.Empty).ToLowerInvariant().Trim();
        if (platform != "ios" && platform != "android" && platform != "web")
            return (false, "Platform 'ios', 'android' veya 'web' olmali.");

        var now = DateTime.UtcNow;
        var existing = await _pushTokens.GetAllQueryable()
            .FirstOrDefaultAsync(t => t.Token == request.Token);

        if (existing != null)
        {
            existing.PlatformUserId = platformUserId;
            existing.Platform = platform;
            existing.DeviceId = request.DeviceId;
            existing.IsActive = true;
            existing.LastUsedAt = now;
            existing.UpdatedAt = now;
            _pushTokens.Update(existing);
        }
        else
        {
            if (!string.IsNullOrEmpty(request.DeviceId))
            {
                var oldDeviceTokens = await _pushTokens.GetAllQueryable()
                    .Where(t => t.PlatformUserId == platformUserId && t.DeviceId == request.DeviceId && t.IsActive)
                    .ToListAsync();

                foreach (var old in oldDeviceTokens)
                {
                    old.IsActive = false;
                    old.UpdatedAt = now;
                    _pushTokens.Update(old);
                }
            }

            _pushTokens.Add(new PlatformPushToken
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

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnregisterAsync(int platformUserId, string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return (false, "Token zorunlu.");

        var entry = await _pushTokens.GetAllQueryable()
            .FirstOrDefaultAsync(t => t.Token == token && t.PlatformUserId == platformUserId);
        if (entry == null) return (false, "Token bulunamadi.");

        entry.IsActive = false;
        entry.UpdatedAt = DateTime.UtcNow;
        _pushTokens.Update(entry);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<PlatformPushTokenDto>> ListAsync(int platformUserId)
    {
        return await _pushTokens.GetAllQueryable()
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
    }
}

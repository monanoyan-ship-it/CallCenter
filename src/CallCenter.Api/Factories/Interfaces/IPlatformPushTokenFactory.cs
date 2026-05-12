using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IPlatformPushTokenFactory
{
    Task<(bool Success, string? Error)> RegisterAsync(int platformUserId, PlatformPushTokenRequest request);
    Task<(bool Success, string? Error)> UnregisterAsync(int platformUserId, string token);
    Task<List<PlatformPushTokenDto>> ListAsync(int platformUserId);
}

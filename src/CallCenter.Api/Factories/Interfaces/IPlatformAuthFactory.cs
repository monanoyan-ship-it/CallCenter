using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IPlatformAuthFactory
{
    Task<(PlatformAuthResponse? Result, string? Error)> RegisterAsync(PlatformRegisterDto dto);
    Task<(PlatformAuthResponse? Result, string? Error)> LoginAsync(PlatformLoginDto dto);
    Task<PlatformUserDto?> GetMeAsync(int platformUserId);
    Task<PlatformUserDto?> UpdateMeAsync(int platformUserId, PlatformUserUpdateDto dto);
    Task<PlatformUserDto?> UpdateBillingInfoAsync(int platformUserId, PlatformBillingUpdateDto dto);
}

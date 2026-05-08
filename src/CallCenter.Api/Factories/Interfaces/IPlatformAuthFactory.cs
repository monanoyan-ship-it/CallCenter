using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IPlatformAuthFactory
{
    Task<(PlatformAuthResponse? Result, string? Error)> RegisterAsync(PlatformRegisterDto dto);
    Task<(PlatformAuthResponse? Result, string? Error)> LoginAsync(PlatformLoginDto dto);
    Task<PlatformUserDto?> GetMeAsync(int platformUserId);
    Task<PlatformUserDto?> UpdateMeAsync(int platformUserId, PlatformUserUpdateDto dto);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int platformUserId, PlatformChangePasswordDto dto);
    Task<PlatformUserDto?> UpdateBillingInfoAsync(int platformUserId, PlatformBillingUpdateDto dto);

    /// <summary>Email dogrulama maili gonder (kayitli email uzerinden).</summary>
    Task<(bool Success, string? Error)> SendVerificationEmailAsync(string email);

    /// <summary>Token ile email dogrulamasini tamamla.</summary>
    Task<(bool Success, string? Error)> VerifyEmailAsync(string token);

    /// <summary>Sifre sifirlama maili gonder (kayitli email uzerinden).</summary>
    Task<(bool Success, string? Error)> SendPasswordResetEmailAsync(string email);

    /// <summary>Token ile sifreyi sifirla.</summary>
    Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword);
}

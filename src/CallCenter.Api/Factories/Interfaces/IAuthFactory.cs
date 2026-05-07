using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IAuthFactory
{
    Task<(bool Success, LoginResponse? Response, string? Error)> LoginAsync(LoginRequest request);
    Task<(bool Success, LoginResponse? Response, string? Error)> RefreshCurrentSessionAsync(int userId);
    Task<(bool Success, RefreshTokenResponse? Response, string? Error)> RefreshAsync(string refreshToken);
    Task RevokeAsync(string refreshToken);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<(bool Success, string? Error)> UpdateLanguageAsync(int userId, string languageCode);

    /// <summary>Email dogrulama maili gonder. UserName uzerinden user bulunur, token uretilir, mail atilir.</summary>
    Task<(bool Success, string? Error)> SendVerificationEmailAsync(string userName);

    /// <summary>Token ile email dogrulamasini tamamla.</summary>
    Task<(bool Success, string? Error)> VerifyEmailAsync(string token);

    /// <summary>Sifre sifirlama maili gonder.</summary>
    Task<(bool Success, string? Error)> SendPasswordResetEmailAsync(string userName);

    /// <summary>Token ile sifreyi sifirla.</summary>
    Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword);
}

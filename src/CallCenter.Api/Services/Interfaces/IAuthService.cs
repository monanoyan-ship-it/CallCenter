using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface IAuthService
{
    Task<(bool Success, LoginResponse? Response, string? Error)> LoginAsync(LoginRequest request);
    Task<(bool Success, RefreshTokenResponse? Response, string? Error)> RefreshAsync(string refreshToken);
    Task RevokeAsync(string refreshToken);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordRequest request, IPasswordPolicyService policyService);
}

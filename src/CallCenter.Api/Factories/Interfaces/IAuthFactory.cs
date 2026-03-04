using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IAuthFactory
{
    Task<(bool Success, LoginResponse? Response, string? Error)> LoginAsync(LoginRequest request);
    Task<(bool Success, RefreshTokenResponse? Response, string? Error)> RefreshAsync(string refreshToken);
    Task RevokeAsync(string refreshToken);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordRequest request);
}

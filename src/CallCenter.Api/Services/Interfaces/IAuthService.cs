using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface IAuthService
{
    Task<(bool Success, LoginResponse? Response, string? Error)> LoginAsync(LoginRequest request);
}

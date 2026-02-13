using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : AuditableControllerBase
{
    public AuthController(ServiceFactory factory) : base(factory) { }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var svc = Factory.CreateAuthService();
        var (success, response, error) = await svc.LoginAsync(request);

        if (!success)
        {
            await AuditAuthAsync("LoginFailed",
                $"Basarisiz giris denemesi: '{request.UserName}' — {error}",
                null, request.UserName);
            return Unauthorized(new { message = error });
        }

        await AuditAuthAsync("Login",
            $"Basarili giris: '{request.UserName}'",
            null, request.UserName);

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh(RefreshTokenRequest request)
    {
        var svc = Factory.CreateAuthService();
        var (success, response, error) = await svc.RefreshAsync(request.RefreshToken);

        if (!success)
        {
            await AuditAuthAsync("RefreshFailed", $"Token yenileme basarisiz: {error}");
            return Unauthorized(new { message = error });
        }

        await AuditAuthAsync("Refresh", "Token yenilendi.");
        return Ok(response);
    }

    [HttpPost("revoke")]
    public async Task<ActionResult> Revoke(RefreshTokenRequest request)
    {
        var svc = Factory.CreateAuthService();
        await svc.RevokeAsync(request.RefreshToken);

        await AuditAuthAsync("Revoke", "Refresh token iptal edildi.");
        return Ok(new { message = "Token iptal edildi." });
    }
}

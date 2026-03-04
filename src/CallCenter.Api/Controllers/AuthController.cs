using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : AuditableControllerBase
{
    private readonly IAuthFactory _authFactory;

    public AuthController(IAuditFactory auditFactory, IAuthFactory authFactory) : base(auditFactory)
    {
        _authFactory = authFactory;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var (success, response, error) = await _authFactory.LoginAsync(request);

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
        var (success, response, error) = await _authFactory.RefreshAsync(request.RefreshToken);

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
        await _authFactory.RevokeAsync(request.RefreshToken);

        await AuditAuthAsync("Revoke", "Refresh token iptal edildi.");
        return Ok(new { message = "Token iptal edildi." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (CurrentUserId == null)
            return Unauthorized();

        var (success, error) = await _authFactory.ChangePasswordAsync(CurrentUserId.Value, request);

        if (!success)
        {
            await AuditAuthAsync("PasswordChangeFailed", $"Sifre degistirme basarisiz: {error}");
            return BadRequest(new { message = error });
        }

        await AuditAuthAsync("PasswordChange", "Sifre basariyla degistirildi.");
        return Ok(new { message = "Şifre başarıyla değiştirildi." });
    }
}

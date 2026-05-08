using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/platform")]
public class PlatformAuthController : ControllerBase
{
    private readonly IPlatformAuthFactory _factory;

    public PlatformAuthController(IPlatformAuthFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Yeni platform kullanicisi kaydi</summary>
    [HttpPost("register")]
    public async Task<ActionResult<PlatformAuthResponse>> Register([FromBody] PlatformRegisterDto dto)
    {
        var (result, error) = await _factory.RegisterAsync(dto);
        if (result == null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>Platform kullanicisi girisi</summary>
    [HttpPost("login")]
    public async Task<ActionResult<PlatformAuthResponse>> Login([FromBody] PlatformLoginDto dto)
    {
        var (result, error) = await _factory.LoginAsync(dto);
        if (result == null) return Unauthorized(new { message = error });
        return Ok(result);
    }

    /// <summary>Mevcut kullanici bilgileri</summary>
    [HttpGet("me")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult<PlatformUserDto>> GetMe()
    {
        var user = await _factory.GetMeAsync(GetPlatformUserId());
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>Profil guncelle</summary>
    [HttpPut("me")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult> UpdateMe([FromBody] PlatformUserUpdateDto dto)
    {
        var result = await _factory.UpdateMeAsync(GetPlatformUserId(), dto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Sifre degistir</summary>
    [HttpPut("me/password")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult> ChangePassword([FromBody] PlatformChangePasswordDto dto)
    {
        var (ok, err) = await _factory.ChangePasswordAsync(GetPlatformUserId(), dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Şifreniz güncellendi." });
    }

    /// <summary>Fatura bilgilerini guncelle</summary>
    [HttpPut("billing-info")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult> UpdateBillingInfo([FromBody] PlatformBillingUpdateDto dto)
    {
        var result = await _factory.UpdateBillingInfoAsync(GetPlatformUserId(), dto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Email dogrulama maili gonder (kayitli email).</summary>
    [HttpPost("send-verification-email")]
    public async Task<ActionResult> SendVerificationEmail([FromBody] PlatformSendVerificationEmailRequest request)
    {
        var (ok, error) = await _factory.SendVerificationEmailAsync(request.Email);
        if (!ok) return BadRequest(new { message = error });
        return Ok(new { message = "Doğrulama maili gönderildi." });
    }

    /// <summary>Token ile email dogrula.</summary>
    [HttpGet("verify-email")]
    public async Task<ActionResult> VerifyEmail([FromQuery] string token)
    {
        var (ok, error) = await _factory.VerifyEmailAsync(token);
        if (!ok) return BadRequest(new { message = error });
        return Ok(new { message = "Email başarıyla doğrulandı." });
    }

    /// <summary>Sifre sifirlama maili iste.</summary>
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] PlatformForgotPasswordRequest request)
    {
        await _factory.SendPasswordResetEmailAsync(request.Email);
        return Ok(new { message = "Eğer hesap mevcutsa, şifre sıfırlama maili gönderildi." });
    }

    /// <summary>Token ile sifreyi sifirla.</summary>
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] PlatformResetPasswordRequest request)
    {
        var (ok, error) = await _factory.ResetPasswordAsync(request.Token, request.NewPassword);
        if (!ok) return BadRequest(new { message = error });
        return Ok(new { message = "Şifre başarıyla güncellendi." });
    }

    private int GetPlatformUserId()
        => int.Parse(User.FindFirstValue("PlatformUserId") ?? "0");
}

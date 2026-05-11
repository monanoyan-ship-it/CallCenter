using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Factories;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : AuditableControllerBase
{
    private readonly IAuthFactory _authFactory;
    private readonly ICustomerFactory _customerFactory;
    private readonly ISubscriptionFactory _subscriptionFactory;
    private readonly ServicePricingFactory _servicePricingFactory;

    public AuthController(
        IAuditFactory auditFactory,
        IAuthFactory authFactory,
        ICustomerFactory customerFactory,
        ISubscriptionFactory subscriptionFactory,
        ServicePricingFactory servicePricingFactory) : base(auditFactory)
    {
        _authFactory = authFactory;
        _customerFactory = customerFactory;
        _subscriptionFactory = subscriptionFactory;
        _servicePricingFactory = servicePricingFactory;
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

    [Authorize]
    [HttpPost("refresh-current")]
    public async Task<ActionResult<LoginResponse>> RefreshCurrentSession()
    {
        if (CurrentUserId == null)
            return Unauthorized();

        var (success, response, error) = await _authFactory.RefreshCurrentSessionAsync(CurrentUserId.Value);
        if (!success)
        {
            await AuditAuthAsync("RefreshCurrentFailed", $"Mevcut oturum yenileme basarisiz: {error}", CurrentUserId);
            return Unauthorized(new { message = error });
        }

        await AuditAuthAsync("RefreshCurrent", "Mevcut oturum token'i yenilendi.", CurrentUserId);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("session-active")]
    public async Task<ActionResult<SessionActiveResponse>> SessionActive(RefreshTokenRequest request)
    {
        if (CurrentUserId == null)
            return Unauthorized();

        var isActive = await _authFactory.IsRefreshTokenActiveAsync(CurrentUserId.Value, request.RefreshToken);
        return Ok(new SessionActiveResponse
        {
            IsActive = isActive,
            Message = isActive ? null : "Bu oturum artik aktif degil. Kullanici baska bir cihazda oturum acmis olabilir."
        });
    }

    [HttpPost("revoke")]
    public async Task<ActionResult> Revoke(RefreshTokenRequest request)
    {
        await _authFactory.RevokeAsync(request.RefreshToken);

        await AuditAuthAsync("Revoke", "Refresh token iptal edildi.");
        return Ok(new { message = "Token iptal edildi." });
    }

    [HttpPost("salon-register")]
    public async Task<ActionResult<SlnRegisterResponse>> SalonRegister(SlnRegisterRequest request)
    {
        var result = await _customerFactory.SalonRegisterAsync(request);

        if (!result.Success && result.Error != null)
        {
            await AuditAuthAsync("SalonRegisterFailed", $"Salon kayit basarisiz: {result.Error}", null, request.UserName);
            return BadRequest(result);
        }

        await AuditAuthAsync("SalonRegister", $"Yeni salon kaydi: '{request.SalonName}' — {request.UserName}", null, request.UserName);
        return Ok(result);
    }

    [HttpGet("salon-register/options")]
    public async Task<ActionResult> GetSalonRegisterOptions()
    {
        var prices = await _servicePricingFactory.GetActiveSalonPackagePricesAsync();
        var coreMonthly = prices.TryGetValue(SalonModuleGroups.Ids.Core, out var price)
            ? price
            : SalonModuleGroups.Core.MonthlyPrice;
        if (coreMonthly <= 0m)
            coreMonthly = SalonModuleGroups.Core.MonthlyPrice;

        var plans = await _subscriptionFactory.GetPlansAsync();
        var activePlans = plans
            .Where(p => p.IsActive && p.IntervalMonths > 0)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.IntervalMonths)
            .Select(p =>
            {
                var periodPrice = Math.Round(coreMonthly * p.IntervalMonths * (1 - p.DiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
                var monthlyEquivalent = Math.Round(periodPrice / p.IntervalMonths, 2, MidpointRounding.AwayFromZero);

                return new
                {
                    p.Id,
                    Name = string.IsNullOrWhiteSpace(p.Name) ? $"{p.IntervalMonths} Aylik Abonelik" : p.Name,
                    p.IntervalMonths,
                    p.DiscountPercent,
                    MonthlyListPrice = coreMonthly,
                    PeriodPrice = periodPrice,
                    MonthlyEquivalent = monthlyEquivalent,
                    IsDefault = p.IntervalMonths == 1
                };
            })
            .ToList();

        return Ok(new
        {
            TrialDays = PlatformBillingAccessPolicy.RegistrationTrialDays,
            FirstPaymentDueInDays = PlatformBillingAccessPolicy.RegistrationTrialDays,
            Currency = "TRY",
            TaxIncluded = true,
            CoreMonthlyPrice = coreMonthly,
            Plans = activePlans
        });
    }

    [Authorize]
    [HttpPut("language")]
    public async Task<ActionResult> UpdateLanguage([FromBody] UpdateLanguageRequest request)
    {
        if (CurrentUserId == null)
            return Unauthorized();

        var (success, error) = await _authFactory.UpdateLanguageAsync(CurrentUserId.Value, request.LanguageCode);

        if (!success)
            return BadRequest(new { message = error });

        return NoContent();
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

    [HttpPost("send-verification-email")]
    public async Task<ActionResult> SendVerificationEmail(SendVerificationEmailRequest request)
    {
        var (success, error) = await _authFactory.SendVerificationEmailAsync(request.UserName);

        if (!success)
        {
            await AuditAuthAsync("VerificationEmailFailed", $"Dogrulama maili gonderilemedi: {error}", null, request.UserName);
            return BadRequest(new { message = error });
        }

        await AuditAuthAsync("VerificationEmailSent", $"Dogrulama maili gonderildi: '{request.UserName}'", null, request.UserName);
        return Ok(new { message = "Doğrulama maili gönderildi." });
    }

    [HttpGet("verify-email")]
    public async Task<ActionResult> VerifyEmail([FromQuery] string token)
    {
        var (success, error) = await _authFactory.VerifyEmailAsync(token);

        if (!success)
        {
            await AuditAuthAsync("EmailVerifyFailed", $"Email dogrulama basarisiz: {error}");
            return BadRequest(new { message = error });
        }

        await AuditAuthAsync("EmailVerified", "Email dogrulandi.");
        return Ok(new { message = "Email başarıyla doğrulandı." });
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _authFactory.SendPasswordResetEmailAsync(request.UserName);
        await AuditAuthAsync("PasswordResetRequested", $"Sifre sifirlama istendi: '{request.UserName}'", null, request.UserName);
        return Ok(new { message = "Eğer hesap mevcutsa, şifre sıfırlama maili gönderildi." });
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var (success, error) = await _authFactory.ResetPasswordAsync(request.Token, request.NewPassword);

        if (!success)
        {
            await AuditAuthAsync("PasswordResetFailed", $"Sifre sifirlama basarisiz: {error}");
            return BadRequest(new { message = error });
        }

        await AuditAuthAsync("PasswordReset", "Sifre token ile sifirlandi.");
        return Ok(new { message = "Şifre başarıyla güncellendi." });
    }
}

using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Helpers;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Api.Services.Email;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CallCenter.Api.Factories;

public class PlatformAuthFactory : IPlatformAuthFactory
{
    private readonly IPlatformUserEntityService _userEs;
    private readonly TokenService _tokenService;
    private readonly IUnitOfWork _uow;
    private readonly IPlatformEmailService _email;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;

    private const int VerificationResendCooldownMinutes = 5;
    private const int PasswordResetTokenHours = 1;
    private const int DailyEmailLimit = 10;

    public PlatformAuthFactory(
        IPlatformUserEntityService userEs,
        TokenService tokenService,
        IUnitOfWork uow,
        IPlatformEmailService email,
        IConfiguration config,
        IMemoryCache cache)
    {
        _userEs = userEs;
        _tokenService = tokenService;
        _uow = uow;
        _email = email;
        _config = config;
        _cache = cache;
    }

    public async Task<(PlatformAuthResponse? Result, string? Error)> RegisterAsync(PlatformRegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(dto.Password))
            return (null, "Telefon ve şifre zorunludur.");

        var normalizedPhone = CallCenter.Shared.Helpers.PhoneHelper.Normalize(dto.Phone) ?? "";
        if (string.IsNullOrEmpty(normalizedPhone))
            return (null, "Geçerli bir telefon numarası giriniz.");

        var exists = await _userEs.GetAllQueryable().AnyAsync(u => u.Phone == normalizedPhone);
        if (exists)
            return (null, "Bu telefon numarası zaten kayıtlı.");

        if (!string.IsNullOrEmpty(dto.Email))
        {
            var emailExists = await _userEs.GetAllQueryable().AnyAsync(u => u.Email == dto.Email);
            if (emailExists)
                return (null, "Bu e-posta adresi zaten kayıtlı.");
        }

        var user = new PlatformUser
        {
            FullName = dto.FullName.Trim(),
            Phone = normalizedPhone,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsPhoneVerified = false,
            IsEmailVerified = false
        };

        _userEs.Add(user);
        await _uow.SaveChangesAsync();

        // Email doğrulama zorunlu mu? Default false → mail tetikleme + token döndür (eski davranış).
        var requireEmailVerify = _config.GetValue<bool>("Auth:RequireEmailVerification:PlatformUser", false);

        if (requireEmailVerify && !string.IsNullOrWhiteSpace(user.Email))
        {
            // Mail tetikle, token DÖNDÜRME — kullanıcı önce email doğrulasın, sonra login olsun.
            try { await SendVerificationEmailAsync(user.Email); } catch { /* sessiz */ }
            return (new PlatformAuthResponse
            {
                Token = string.Empty,
                RequiresEmailVerification = true,
                User = MapToDto(user)
            }, null);
        }

        var token = _tokenService.GeneratePlatformToken(user);
        return (new PlatformAuthResponse
        {
            Token = token,
            RequiresEmailVerification = false,
            User = MapToDto(user)
        }, null);
    }

    public async Task<(PlatformAuthResponse? Result, string? Error)> LoginAsync(PlatformLoginDto dto)
    {
        var normalizedPhone = CallCenter.Shared.Helpers.PhoneHelper.Normalize(dto.Phone) ?? "";
        var user = await _userEs.GetAllQueryable()
            .Include(u => u.Salons)
            .FirstOrDefaultAsync(u => u.Phone == normalizedPhone);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return (null, "Telefon veya şifre hatalı.");

        if (!user.IsActive)
            return (null, "Hesabınız devre dışı bırakılmış.");

        // Auth:RequireEmailVerification:PlatformUser=true ise mail doğrulaması zorunlu. Default false.
        var requireEmailVerify = _config.GetValue<bool>("Auth:RequireEmailVerification:PlatformUser", false);
        if (requireEmailVerify && !string.IsNullOrWhiteSpace(user.Email) && !user.IsEmailVerified)
            return (null, "Email adresinizi doğrulayın. Mail kutunuza gönderilen bağlantı üzerinden hesabınızı aktif edin.");

        user.LastLoginAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        var token = _tokenService.GeneratePlatformToken(user);
        return (new PlatformAuthResponse
        {
            Token = token,
            RequiresEmailVerification = false,
            User = MapToDto(user)
        }, null);
    }

    public async Task<PlatformUserDto?> GetMeAsync(int platformUserId)
    {
        var user = await _userEs.GetAllQueryable()
            .Include(u => u.Salons)
            .FirstOrDefaultAsync(u => u.Id == platformUserId);

        return user == null ? null : MapToDto(user);
    }

    public async Task<PlatformUserDto?> UpdateMeAsync(int platformUserId, PlatformUserUpdateDto dto)
    {
        var user = await _userEs.GetByIdAsync(platformUserId);
        if (user == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName.Trim();
        if (dto.Email != null) user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;

        await _uow.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int platformUserId, PlatformChangePasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return (false, "Mevcut ve yeni şifre zorunludur.");

        if (dto.NewPassword.Length < 6)
            return (false, "Yeni şifre en az 6 karakter olmalıdır.");

        var user = await _userEs.GetByIdAsync(platformUserId);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return (false, "Mevcut şifre hatalı.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<PlatformUserDto?> UpdateBillingInfoAsync(int platformUserId, PlatformBillingUpdateDto dto)
    {
        var user = await _userEs.GetByIdAsync(platformUserId);
        if (user == null) return null;

        user.BillingType = dto.BillingType;
        user.BillingFullName = dto.BillingFullName;
        user.BillingCompanyName = dto.BillingCompanyName;
        user.BillingTaxOffice = dto.BillingTaxOffice;
        user.BillingTaxNumber = dto.BillingTaxNumber;
        user.BillingAddress = dto.BillingAddress;
        user.BillingCity = dto.BillingCity;
        user.BillingDistrict = dto.BillingDistrict;
        user.BillingPostalCode = dto.BillingPostalCode;

        await _uow.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<(bool Success, string? Error)> SendVerificationEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Email zorunlu.");

        var user = await _userEs.GetAllQueryable()
            .FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");
        if (user.IsEmailVerified)
            return (false, "Email zaten doğrulanmış.");

        if (user.EmailVerificationSentAt.HasValue &&
            (DateTime.UtcNow - user.EmailVerificationSentAt.Value).TotalMinutes < VerificationResendCooldownMinutes)
        {
            return (false, $"Lütfen {VerificationResendCooldownMinutes} dakika içinde tekrar deneyin.");
        }

        if (!TryIncrementDailyEmailCount(email, "platform_verify"))
            return (false, "Günlük doğrulama mail limiti aşıldı. Lütfen yarın tekrar deneyin.");

        var token = Guid.NewGuid().ToString("N");
        user.EmailVerificationToken = token;
        user.EmailVerificationSentAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        var baseUrl = (_config["Salon:BaseUrl"] ?? "https://sln.corplynk.com").TrimEnd('/');
        var link = $"{baseUrl}/user/verify-email?token={token}";
        var lang = NormalizeLanguage(user.PreferredLanguage);
        var placeholders = new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["VerifyUrl"] = link
        };

        var ok = await _email.SendTemplatedAsync(user.Email!, PlatformEmailSeedHelper.EventPlatformUserEmailVerify, placeholders, lang);
        if (!ok)
        {
            var (subject, html) = BuildVerificationEmailBody(user.FullName, link, lang);
            ok = await _email.SendAsync(user.Email!, user.FullName, subject, html);
        }
        return ok ? (true, null) : (false, "Email gönderilemedi.");
    }

    public async Task<(bool Success, string? Error)> VerifyEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, "Geçersiz token.");

        var user = await _userEs.GetAllQueryable()
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        if (user == null)
            return (false, "Token geçersiz veya kullanılmış.");

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationSentAt = null;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SendPasswordResetEmailAsync(string email)
    {
        // Bilgi sızdırmayı önlemek için kullanıcı yoksa da başarı dön.
        if (string.IsNullOrWhiteSpace(email))
            return (true, null);

        var user = await _userEs.GetAllQueryable()
            .FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !user.IsActive)
            return (true, null);

        if (user.PasswordResetSentAt.HasValue &&
            (DateTime.UtcNow - user.PasswordResetSentAt.Value).TotalMinutes < VerificationResendCooldownMinutes)
        {
            return (true, null);
        }

        if (!TryIncrementDailyEmailCount(email, "platform_reset"))
            return (true, null);

        var token = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = token;
        user.PasswordResetSentAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        var baseUrl = (_config["Salon:BaseUrl"] ?? "https://sln.corplynk.com").TrimEnd('/');
        var link = $"{baseUrl}/user/reset-password?token={token}";
        var lang = NormalizeLanguage(user.PreferredLanguage);
        var placeholders = new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["ResetUrl"] = link
        };

        var ok = await _email.SendTemplatedAsync(user.Email!, PlatformEmailSeedHelper.EventPlatformUserPasswordReset, placeholders, lang);
        if (!ok)
        {
            var (subject, html) = BuildPasswordResetEmailBody(user.FullName, link, lang);
            await _email.SendAsync(user.Email!, user.FullName, subject, html);
        }
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, "Geçersiz token.");
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Şifre en az 6 karakter olmalı.");

        var user = await _userEs.GetAllQueryable()
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
        if (user == null)
            return (false, "Token geçersiz veya kullanılmış.");

        if (user.PasswordResetSentAt == null ||
            (DateTime.UtcNow - user.PasswordResetSentAt.Value).TotalHours > PasswordResetTokenHours)
        {
            user.PasswordResetToken = null;
            user.PasswordResetSentAt = null;
            await _uow.SaveChangesAsync();
            return (false, "Token süresi dolmuş.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetSentAt = null;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private bool TryIncrementDailyEmailCount(string email, string scope)
    {
        var key = $"platform_email_count:{scope}:{email.ToLowerInvariant()}:{DateTime.UtcNow:yyyyMMdd}";
        var current = _cache.Get<int?>(key) ?? 0;
        if (current >= DailyEmailLimit) return false;
        _cache.Set(key, current + 1, TimeSpan.FromHours(24));
        return true;
    }

    private static string NormalizeLanguage(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return "tr";
        return lang.ToLowerInvariant() == "en" ? "en" : "tr";
    }

    private static (string Subject, string Html) BuildVerificationEmailBody(string fullName, string link, string lang)
    {
        if (lang == "en")
        {
            return ("Verify your CorpLynk account", $"""
                <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
                    <h2 style="color:#7b1fa2;">Welcome, {System.Net.WebUtility.HtmlEncode(fullName)}</h2>
                    <p>Please verify your email address to activate your account.</p>
                    <p style="margin:24px 0;"><a href="{link}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Verify email</a></p>
                    <p style="color:#6c757d;font-size:13px;">If the button does not work, copy this link:<br/><span style="word-break:break-all;">{link}</span></p>
                </div>
                """);
        }
        return ("CorpLynk hesabını doğrula", $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
                <h2 style="color:#7b1fa2;">Hoş geldin, {System.Net.WebUtility.HtmlEncode(fullName)}</h2>
                <p>Hesabını aktif etmek için email adresini doğrula.</p>
                <p style="margin:24px 0;"><a href="{link}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Email'i doğrula</a></p>
                <p style="color:#6c757d;font-size:13px;">Düğme çalışmıyorsa bu bağlantıyı kopyala:<br/><span style="word-break:break-all;">{link}</span></p>
            </div>
            """);
    }

    private static (string Subject, string Html) BuildPasswordResetEmailBody(string fullName, string link, string lang)
    {
        if (lang == "en")
        {
            return ("Reset your CorpLynk password", $"""
                <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
                    <h2 style="color:#7b1fa2;">Password reset</h2>
                    <p>Hello {System.Net.WebUtility.HtmlEncode(fullName)}, click below to set a new password. The link is valid for 1 hour.</p>
                    <p style="margin:24px 0;"><a href="{link}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Reset password</a></p>
                    <p style="color:#6c757d;font-size:13px;">If you did not request this, you can ignore this email.</p>
                </div>
                """);
        }
        return ("CorpLynk şifre sıfırlama", $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
                <h2 style="color:#7b1fa2;">Şifre sıfırlama</h2>
                <p>Merhaba {System.Net.WebUtility.HtmlEncode(fullName)}, yeni şifre belirlemek için butona tıkla. Bağlantı 1 saat geçerli.</p>
                <p style="margin:24px 0;"><a href="{link}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Şifreyi sıfırla</a></p>
                <p style="color:#6c757d;font-size:13px;">Bu işlemi sen başlatmadıysan emaili görmezden gelebilirsin.</p>
            </div>
            """);
    }

    private static PlatformUserDto MapToDto(PlatformUser u) => new()
    {
        Uid = u.Uid,
        FullName = u.FullName,
        Phone = u.Phone,
        Email = u.Email,
        IsEmailVerified = u.IsEmailVerified,
        AvatarUrl = u.AvatarUrl,
        SalonCount = u.Salons?.Count(s => s.IsActive) ?? 0,
        BillingType = u.BillingType,
        BillingFullName = u.BillingFullName,
        BillingCompanyName = u.BillingCompanyName,
        BillingTaxOffice = u.BillingTaxOffice,
        BillingTaxNumber = u.BillingTaxNumber,
        BillingAddress = u.BillingAddress,
        BillingCity = u.BillingCity,
        BillingDistrict = u.BillingDistrict,
        BillingPostalCode = u.BillingPostalCode
    };
}

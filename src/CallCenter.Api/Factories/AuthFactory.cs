using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Hubs;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Api.Services.Email;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace CallCenter.Api.Factories;

public class AuthFactory : IAuthFactory
{
    private readonly IUserEntityService _users;
    private readonly IRefreshTokenEntityService _refreshTokens;
    private readonly ICustomerEntityService _customers;
    private readonly ICustomerPortalModuleEntityService _portalModules;
    private readonly IPasswordPolicyFactory _passwordPolicy;
    private readonly IBillingFactory _billingFactory;
    private readonly TokenService _tokenService;
    private readonly IConfiguration _config;
    private readonly IHubContext<CallCenterHub> _hubContext;
    private readonly IUnitOfWork _uow;
    private readonly IPlatformEmailService _email;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;
    private const int PasswordResetTokenHours = 1;
    private const int VerificationResendCooldownMinutes = 5;
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase) { "tr", "en", "de", "ar", "ru" };

    public AuthFactory(
        IUserEntityService users,
        IRefreshTokenEntityService refreshTokens,
        ICustomerEntityService customers,
        ICustomerPortalModuleEntityService portalModules,
        IPasswordPolicyFactory passwordPolicy,
        IBillingFactory billingFactory,
        TokenService tokenService,
        IConfiguration config,
        IHubContext<CallCenterHub> hubContext,
        IUnitOfWork uow,
        IPlatformEmailService email)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _customers = customers;
        _portalModules = portalModules;
        _passwordPolicy = passwordPolicy;
        _billingFactory = billingFactory;
        _tokenService = tokenService;
        _config = config;
        _hubContext = hubContext;
        _uow = uow;
        _email = email;
    }

    public async Task<(bool Success, LoginResponse? Response, string? Error)> LoginAsync(LoginRequest request)
    {
        var user = await _users.GetByUsernameWithPersonnelAsync(request.UserName);

        if (user == null)
            return (false, null, "Kullanıcı adı veya şifre hatalı.");

        if (!user.IsActive)
            return (false, null, "Kullanıcı hesabı aktif değil.");

        if (!user.IsEmailVerified && !string.IsNullOrWhiteSpace(user.Email))
            return (false, null, "Email adresinizi doğrulayın. Mail kutunuza gönderilen bağlantı üzerinden hesabınızı aktif edin.");

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            var remaining = (int)(user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes + 1;
            return (false, null, $"Hesap kilitli. {remaining} dakika sonra tekrar deneyin.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                user.FailedLoginCount = 0;
                await _uow.SaveChangesAsync();
                return (false, null, $"Çok fazla hatalı deneme. Hesap {LockoutMinutes} dakika kilitlendi.");
            }

            await _uow.SaveChangesAsync();
            return (false, null, "Kullanıcı adı veya şifre hatalı.");
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

        if (user.CustomerPersonnel != null)
        {
            var customer = await _customers.GetByIdAsync(user.CustomerPersonnel.CustomerId);
            if (customer == null || !customer.IsActive)
                return (false, null, "Müşteri hesabı aktif değil.");

            var (isBlocked, reason) = await _billingFactory.IsCustomerBlockedByBillingAsync(user.CustomerPersonnel.CustomerId);
            if (isBlocked)
                return (false, null, reason ?? "Odenmemis fatura nedeniyle erisim engellendi.");
        }

        List<int>? moduleIds = null;
        if (user.CustomerPersonnel != null)
            moduleIds = await _portalModules.GetActiveModuleIdsAsync(user.CustomerPersonnel.CustomerId);

        var token = _tokenService.GenerateToken(user, user.CustomerPersonnel, moduleIds);
        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "480");

        var oldTokens = await _refreshTokens.GetActiveByUserIdAsync(user.Id);
        foreach (var old in oldTokens)
            old.RevokedAt = DateTime.UtcNow;

        await _hubContext.Clients.User(user.Id.ToString()).SendAsync("ForceLogout");

        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);
        _refreshTokens.Add(refreshToken);

        await _uow.SaveChangesAsync();

        var roleName = UserRoles.GetById(user.RoleId)?.SystemName ?? "Agent";

        return (true, new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            FullName = user.FullName,
            Role = roleName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes),
            MustChangePassword = user.MustChangePassword,
            PreferredLanguage = user.PreferredLanguage
        }, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return (false, "Mevcut şifre hatalı.");

        var (isValid, errors) = _passwordPolicy.ValidatePassword(request.NewPassword);
        if (!isValid)
            return (false, string.Join(" ", errors));

        if (await _passwordPolicy.IsPasswordReusedAsync(userId, request.NewPassword))
            return (false, "Bu şifre daha önce kullanılmış. Farklı bir şifre seçiniz.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordHash = newHash;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;

        await _uow.SaveChangesAsync();
        await _passwordPolicy.RecordPasswordAsync(userId, newHash);

        return (true, null);
    }

    public async Task<(bool Success, LoginResponse? Response, string? Error)> RefreshCurrentSessionAsync(int userId)
    {
        var user = await _users.GetAllQueryable()
            .Include(u => u.CustomerPersonnel)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return (false, null, "Kullanici bulunamadi.");

        if (!user.IsActive)
            return (false, null, "Kullanici hesabi aktif degil.");

        if (user.CustomerPersonnel != null)
        {
            var customer = await _customers.GetByIdAsync(user.CustomerPersonnel.CustomerId);
            if (customer == null || !customer.IsActive)
                return (false, null, "Musteri hesabi aktif degil.");

            var (isBlocked, reason) = await _billingFactory.IsCustomerBlockedByBillingAsync(user.CustomerPersonnel.CustomerId);
            if (isBlocked)
                return (false, null, reason ?? "Odenmemis fatura nedeniyle erisim engellendi.");
        }

        List<int>? moduleIds = null;
        if (user.CustomerPersonnel != null)
            moduleIds = await _portalModules.GetActiveModuleIdsAsync(user.CustomerPersonnel.CustomerId);

        var token = _tokenService.GenerateToken(user, user.CustomerPersonnel, moduleIds);
        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "480");
        var roleName = UserRoles.GetById(user.RoleId)?.SystemName ?? "Agent";

        return (true, new LoginResponse
        {
            Token = token,
            FullName = user.FullName,
            Role = roleName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes),
            MustChangePassword = user.MustChangePassword,
            PreferredLanguage = user.PreferredLanguage
        }, null);
    }

    public async Task<(bool Success, RefreshTokenResponse? Response, string? Error)> RefreshAsync(string refreshToken)
    {
        var existingToken = await _refreshTokens.GetByTokenWithUserAsync(refreshToken);

        if (existingToken == null)
            return (false, null, "Gecersiz refresh token.");

        if (existingToken.IsRevoked)
        {
            await RevokeDescendantTokensAsync(existingToken);
            return (false, null, "Refresh token iptal edilmis.");
        }

        if (existingToken.IsExpired)
            return (false, null, "Refresh token suresi dolmus.");

        var user = existingToken.User;

        if (!user.IsActive)
            return (false, null, "Kullanici hesabi aktif degil.");

        if (user.CustomerPersonnel != null)
        {
            var customer = await _customers.GetByIdAsync(user.CustomerPersonnel.CustomerId);
            if (customer == null || !customer.IsActive)
                return (false, null, "Musteri hesabi aktif degil.");

            var (isBlocked, reason) = await _billingFactory.IsCustomerBlockedByBillingAsync(user.CustomerPersonnel.CustomerId);
            if (isBlocked)
                return (false, null, reason ?? "Odenmemis fatura nedeniyle erisim engellendi.");
        }

        existingToken.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id);
        existingToken.ReplacedByToken = newRefreshToken.Token;
        _refreshTokens.Add(newRefreshToken);

        List<int>? refreshModuleIds = null;
        if (user.CustomerPersonnel != null)
            refreshModuleIds = await _portalModules.GetActiveModuleIdsAsync(user.CustomerPersonnel.CustomerId);

        var accessToken = _tokenService.GenerateToken(user, user.CustomerPersonnel, refreshModuleIds);
        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "480");

        await _uow.SaveChangesAsync();

        return (true, new RefreshTokenResponse
        {
            Token = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes)
        }, null);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        var token = await _refreshTokens.GetByTokenAsync(refreshToken);

        if (token != null && token.IsActive)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
        }
    }

    public async Task<(bool Success, string? Error)> UpdateLanguageAsync(int userId, string languageCode)
    {
        if (!SupportedLanguages.Contains(languageCode))
            return (false, "Desteklenmeyen dil kodu.");

        var user = await _users.GetByIdAsync(userId);
        if (user == null)
            return (false, "Kullanici bulunamadi.");

        user.PreferredLanguage = languageCode.ToLowerInvariant();
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private async Task RevokeDescendantTokensAsync(Shared.Entities.RefreshToken token)
    {
        if (string.IsNullOrEmpty(token.ReplacedByToken)) return;

        var childToken = await _refreshTokens.GetByTokenAsync(token.ReplacedByToken);

        if (childToken == null) return;

        if (childToken.IsActive)
        {
            childToken.RevokedAt = DateTime.UtcNow;
        }
        else
        {
            await RevokeDescendantTokensAsync(childToken);
        }

        await _uow.SaveChangesAsync();
    }

    public async Task<(bool Success, string? Error)> SendVerificationEmailAsync(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return (false, "Kullanıcı adı zorunlu.");

        var user = await _users.GetByUsernameAsync(userName);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        if (string.IsNullOrWhiteSpace(user.Email))
            return (false, "Kullanıcının email adresi kayıtlı değil.");

        if (user.IsEmailVerified)
            return (false, "Email zaten doğrulanmış.");

        if (user.EmailVerificationSentAt.HasValue &&
            (DateTime.UtcNow - user.EmailVerificationSentAt.Value).TotalMinutes < VerificationResendCooldownMinutes)
        {
            return (false, $"Lütfen {VerificationResendCooldownMinutes} dakika içinde tekrar deneyin.");
        }

        var token = Guid.NewGuid().ToString("N");
        user.EmailVerificationToken = token;
        user.EmailVerificationSentAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        var baseUrl = (_config["Salon:BaseUrl"] ?? "https://sln.corplynk.com").TrimEnd('/');
        var link = $"{baseUrl}/Account/VerifyEmail?token={token}";
        var lang = NormalizeLanguage(user.PreferredLanguage);
        var (subject, html) = BuildVerificationEmailBody(user.FullName, link, lang);

        var ok = await _email.SendAsync(user.Email, user.FullName, subject, html);
        return ok ? (true, null) : (false, "Email gönderilemedi.");
    }

    public async Task<(bool Success, string? Error)> VerifyEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, "Geçersiz token.");

        var user = await _users.GetByEmailVerificationTokenAsync(token);
        if (user == null)
            return (false, "Token geçersiz veya kullanılmış.");

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationSentAt = null;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SendPasswordResetEmailAsync(string userName)
    {
        // Bilgi sızdırmasını önlemek için kullanıcı yoksa da başarı dön
        if (string.IsNullOrWhiteSpace(userName))
            return (true, null);

        var user = await _users.GetByUsernameAsync(userName);
        if (user == null || string.IsNullOrWhiteSpace(user.Email) || !user.IsActive)
            return (true, null);

        if (user.PasswordResetSentAt.HasValue &&
            (DateTime.UtcNow - user.PasswordResetSentAt.Value).TotalMinutes < VerificationResendCooldownMinutes)
        {
            return (true, null);
        }

        var token = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = token;
        user.PasswordResetSentAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        var baseUrl = (_config["Salon:BaseUrl"] ?? "https://sln.corplynk.com").TrimEnd('/');
        var link = $"{baseUrl}/Account/ResetPassword?token={token}";
        var lang = NormalizeLanguage(user.PreferredLanguage);
        var (subject, html) = BuildPasswordResetEmailBody(user.FullName, link, lang);

        await _email.SendAsync(user.Email, user.FullName, subject, html);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, "Geçersiz token.");
        if (string.IsNullOrWhiteSpace(newPassword))
            return (false, "Yeni şifre zorunlu.");

        var user = await _users.GetByPasswordResetTokenAsync(token);
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

        var (isValid, errors) = _passwordPolicy.ValidatePassword(newPassword);
        if (!isValid)
            return (false, string.Join(" ", errors));

        if (await _passwordPolicy.IsPasswordReusedAsync(user.Id, newPassword))
            return (false, "Bu şifre daha önce kullanılmış. Farklı bir şifre seçiniz.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordHash = newHash;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.PasswordResetToken = null;
        user.PasswordResetSentAt = null;
        user.MustChangePassword = false;
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        await _uow.SaveChangesAsync();
        await _passwordPolicy.RecordPasswordAsync(user.Id, newHash);
        return (true, null);
    }

    private static string NormalizeLanguage(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return "tr";
        var lower = lang.ToLowerInvariant();
        return lower == "en" ? "en" : "tr";
    }

    private static (string Subject, string Html) BuildVerificationEmailBody(string fullName, string link, string lang)
    {
        if (lang == "en")
        {
            var subject = "Verify your CorpLynk account";
            var html = $"""
                <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
                    <h2 style="color:#7b1fa2;">Welcome, {System.Net.WebUtility.HtmlEncode(fullName)}</h2>
                    <p>Please verify your email address to activate your CorpLynk account.</p>
                    <p style="margin:24px 0;">
                        <a href="{link}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Verify email</a>
                    </p>
                    <p style="color:#6c757d;font-size:13px;">If the button does not work, copy this link: <br/><span style="word-break:break-all;">{link}</span></p>
                </div>
                """;
            return (subject, html);
        }

        var subjectTr = "CorpLynk hesabını doğrula";
        var htmlTr = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
                <h2 style="color:#7b1fa2;">Hoş geldin, {System.Net.WebUtility.HtmlEncode(fullName)}</h2>
                <p>CorpLynk hesabını aktif etmek için email adresini doğrula.</p>
                <p style="margin:24px 0;">
                    <a href="{link}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Email'i doğrula</a>
                </p>
                <p style="color:#6c757d;font-size:13px;">Düğme çalışmıyorsa bu bağlantıyı kopyala:<br/><span style="word-break:break-all;">{link}</span></p>
            </div>
            """;
        return (subjectTr, htmlTr);
    }

    private static (string Subject, string Html) BuildPasswordResetEmailBody(string fullName, string link, string lang)
    {
        if (lang == "en")
        {
            var subject = "Reset your CorpLynk password";
            var html = $"""
                <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
                    <h2 style="color:#7b1fa2;">Password reset</h2>
                    <p>Hello {System.Net.WebUtility.HtmlEncode(fullName)}, click the button below to set a new password. The link is valid for 1 hour.</p>
                    <p style="margin:24px 0;">
                        <a href="{link}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Reset password</a>
                    </p>
                    <p style="color:#6c757d;font-size:13px;">If you did not request this, you can ignore this email.</p>
                </div>
                """;
            return (subject, html);
        }

        var subjectTr = "CorpLynk şifre sıfırlama";
        var htmlTr = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#212529;">
                <h2 style="color:#7b1fa2;">Şifre sıfırlama</h2>
                <p>Merhaba {System.Net.WebUtility.HtmlEncode(fullName)}, yeni şifre belirlemek için butona tıkla. Bağlantı 1 saat geçerli.</p>
                <p style="margin:24px 0;">
                    <a href="{link}" style="background:#7b1fa2;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;">Şifreyi sıfırla</a>
                </p>
                <p style="color:#6c757d;font-size:13px;">Bu işlemi sen başlatmadıysan emaili görmezden gelebilirsin.</p>
            </div>
            """;
        return (subjectTr, htmlTr);
    }
}

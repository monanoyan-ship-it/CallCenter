using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Hubs;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
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

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;
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
        IUnitOfWork uow)
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
    }

    public async Task<(bool Success, LoginResponse? Response, string? Error)> LoginAsync(LoginRequest request)
    {
        var user = await _users.GetByUsernameWithPersonnelAsync(request.UserName);

        if (user == null)
            return (false, null, "Kullanıcı adı veya şifre hatalı.");

        if (!user.IsActive)
            return (false, null, "Kullanıcı hesabı aktif değil.");

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
}

using CallCenter.Api.Hubs;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly IConfiguration _config;
    private readonly IHubContext<CallCenterHub> _hubContext;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    public AuthService(AppDbContext db, TokenService tokenService, IConfiguration config, IHubContext<CallCenterHub> hubContext)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
        _hubContext = hubContext;
    }

    public async Task<(bool Success, LoginResponse? Response, string? Error)> LoginAsync(LoginRequest request)
    {
        // IsActive filtresi kaldirildi — kilitleme/aktiflik ayri kontrol edilecek
        var user = await _db.Users
            .Include(u => u.CustomerPersonnel)
                .ThenInclude(cp => cp!.Permissions)
            .FirstOrDefaultAsync(u => u.UserName == request.UserName);

        // Kullanici bulunamadi — generic hata (username enumeration engelleme)
        if (user == null)
            return (false, null, "Kullanıcı adı veya şifre hatalı.");

        // Hesap aktif mi?
        if (!user.IsActive)
            return (false, null, "Kullanıcı hesabı aktif değil.");

        // Hesap kilitli mi?
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            var remaining = (int)(user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes + 1;
            return (false, null, $"Hesap kilitli. {remaining} dakika sonra tekrar deneyin.");
        }

        // Sifre dogru mu?
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // Basarisiz deneme sayisini artir
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                user.FailedLoginCount = 0; // Kilit sonrasi sifirla
                await _db.SaveChangesAsync();
                return (false, null, $"Çok fazla hatalı deneme. Hesap {LockoutMinutes} dakika kilitlendi.");
            }

            await _db.SaveChangesAsync();
            return (false, null, "Kullanıcı adı veya şifre hatalı.");
        }

        // Basarili giris — sayaclari sifirla
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

        // Musteri kullanicisiysa firma aktiflik kontrolu
        if (user.CustomerPersonnel != null)
        {
            var customer = await _db.Customers.FindAsync(user.CustomerPersonnel.CustomerId);
            if (customer == null || !customer.IsActive)
                return (false, null, "Müşteri hesabı aktif değil.");
        }

        // Musteri personelinin aktif yetkilerini filtrele
        IEnumerable<int>? activePermissionIds = null;
        if (user.CustomerPersonnel != null)
        {
            var now = DateTime.UtcNow;
            activePermissionIds = user.CustomerPersonnel.Permissions
                .Where(p => p.IsActive)
                .Where(p => !p.ValidFrom.HasValue || p.ValidFrom.Value <= now)
                .Where(p => !p.ValidUntil.HasValue || p.ValidUntil.Value >= now)
                .Select(p => p.PermissionTypeId)
                .ToList();
        }

        var token = _tokenService.GenerateToken(user, user.CustomerPersonnel, activePermissionIds);
        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "480");

        // Eski tum aktif refresh token'lari revoke et (tek oturum zorunlulugu)
        var oldTokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var old in oldTokens)
            old.RevokedAt = DateTime.UtcNow;

        // SignalR ile eski baglantilarai ForceLogout gonder
        await _hubContext.Clients.User(user.Id.ToString()).SendAsync("ForceLogout");

        // Refresh token uret ve DB'ye kaydet
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);
        _db.RefreshTokens.Add(refreshToken);

        await _db.SaveChangesAsync();

        var roleName = UserRoles.GetById(user.RoleId)?.SystemName ?? "Agent";

        return (true, new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            FullName = user.FullName,
            Role = roleName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes),
            MustChangePassword = user.MustChangePassword
        }, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordRequest request, IPasswordPolicyService policyService)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        // Mevcut sifre dogrula
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return (false, "Mevcut şifre hatalı.");

        // Yeni sifre politikasini kontrol et
        var (isValid, errors) = policyService.ValidatePassword(request.NewPassword);
        if (!isValid)
            return (false, string.Join(" ", errors));

        // Sifre gecmisi kontrolu
        if (await policyService.IsPasswordReusedAsync(userId, request.NewPassword))
            return (false, "Bu şifre daha önce kullanılmış. Farklı bir şifre seçiniz.");

        // Yeni sifre hash'le ve kaydet
        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordHash = newHash;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;

        await _db.SaveChangesAsync();

        // Sifre gecmisine kaydet
        await policyService.RecordPasswordAsync(userId, newHash);

        return (true, null);
    }

    public async Task<(bool Success, RefreshTokenResponse? Response, string? Error)> RefreshAsync(string refreshToken)
    {
        var existingToken = await _db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.CustomerPersonnel)
                    .ThenInclude(cp => cp!.Permissions)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (existingToken == null)
            return (false, null, "Gecersiz refresh token.");

        if (existingToken.IsRevoked)
        {
            // Calinti token tespiti: Revoke edilmis token kullaniliyorsa
            // bu token zincirindeki tum aktif token'lari iptal et
            await RevokeDescendantTokensAsync(existingToken);
            return (false, null, "Refresh token iptal edilmis.");
        }

        if (existingToken.IsExpired)
            return (false, null, "Refresh token suresi dolmus.");

        var user = existingToken.User;

        if (!user.IsActive)
            return (false, null, "Kullanici hesabi aktif degil.");

        // Musteri kullanicisiysa firma aktiflik kontrolu
        if (user.CustomerPersonnel != null)
        {
            var customer = await _db.Customers.FindAsync(user.CustomerPersonnel.CustomerId);
            if (customer == null || !customer.IsActive)
                return (false, null, "Musteri hesabi aktif degil.");
        }

        // Eski token'i revoke et (rotation)
        existingToken.RevokedAt = DateTime.UtcNow;

        // Yeni refresh token uret
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id);
        existingToken.ReplacedByToken = newRefreshToken.Token;
        _db.RefreshTokens.Add(newRefreshToken);

        // Yeni access token uret (guncel permission'larla)
        IEnumerable<int>? activePermissionIds = null;
        if (user.CustomerPersonnel != null)
        {
            var now = DateTime.UtcNow;
            activePermissionIds = user.CustomerPersonnel.Permissions
                .Where(p => p.IsActive)
                .Where(p => !p.ValidFrom.HasValue || p.ValidFrom.Value <= now)
                .Where(p => !p.ValidUntil.HasValue || p.ValidUntil.Value >= now)
                .Select(p => p.PermissionTypeId)
                .ToList();
        }

        var accessToken = _tokenService.GenerateToken(user, user.CustomerPersonnel, activePermissionIds);
        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "480");

        await _db.SaveChangesAsync();

        return (true, new RefreshTokenResponse
        {
            Token = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes)
        }, null);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token != null && token.IsActive)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Calinti token tespitinde: Revoke edilmis token'in tum torunlarini iptal eder.
    /// </summary>
    private async Task RevokeDescendantTokensAsync(Shared.Entities.RefreshToken token)
    {
        if (string.IsNullOrEmpty(token.ReplacedByToken)) return;

        var childToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token.ReplacedByToken);

        if (childToken == null) return;

        if (childToken.IsActive)
        {
            childToken.RevokedAt = DateTime.UtcNow;
        }
        else
        {
            await RevokeDescendantTokensAsync(childToken);
        }

        await _db.SaveChangesAsync();
    }
}

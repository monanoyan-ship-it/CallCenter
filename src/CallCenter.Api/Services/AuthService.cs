using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, TokenService tokenService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
    }

    public async Task<(bool Success, LoginResponse? Response, string? Error)> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.CustomerPersonnel)
                .ThenInclude(cp => cp!.Permissions)
            .FirstOrDefaultAsync(u => u.UserName == request.UserName && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return (false, null, "Kullanıcı adı veya şifre hatalı.");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

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

        var roleName = UserRoles.GetById(user.RoleId)?.SystemName ?? "Agent";

        return (true, new LoginResponse
        {
            Token = token,
            FullName = user.FullName,
            Role = roleName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes)
        }, null);
    }
}

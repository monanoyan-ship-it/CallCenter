using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.CustomerPersonnel)
                .ThenInclude(cp => cp!.Permissions)
            .FirstOrDefaultAsync(u => u.UserName == request.UserName && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı." });

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
        var expireMinutes = int.Parse(HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Jwt:ExpireMinutes"] ?? "480");

        var roleName = UserRoles.GetById(user.RoleId)?.SystemName ?? "Agent";

        return Ok(new LoginResponse
        {
            Token = token,
            FullName = user.FullName,
            Role = roleName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes)
        });
    }
}

using System.Security.Claims;
using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/platform")]
public class PlatformAuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;

    public PlatformAuthController(AppDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    /// <summary>Yeni platform kullanicisi kaydi</summary>
    [HttpPost("register")]
    public async Task<ActionResult<PlatformAuthResponse>> Register([FromBody] PlatformRegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Telefon ve şifre zorunludur." });

        var exists = await _db.PlatformUsers.AnyAsync(u => u.Phone == dto.Phone);
        if (exists)
            return BadRequest(new { message = "Bu telefon numarası zaten kayıtlı." });

        if (!string.IsNullOrEmpty(dto.Email))
        {
            var emailExists = await _db.PlatformUsers.AnyAsync(u => u.Email == dto.Email);
            if (emailExists)
                return BadRequest(new { message = "Bu e-posta adresi zaten kayıtlı." });
        }

        var user = new PlatformUser
        {
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsPhoneVerified = false,
            IsEmailVerified = false
        };

        _db.PlatformUsers.Add(user);
        await _db.SaveChangesAsync();

        var token = _tokenService.GeneratePlatformToken(user);
        return Ok(new PlatformAuthResponse
        {
            Token = token,
            User = MapToDto(user)
        });
    }

    /// <summary>Platform kullanicisi girisi</summary>
    [HttpPost("login")]
    public async Task<ActionResult<PlatformAuthResponse>> Login([FromBody] PlatformLoginDto dto)
    {
        var user = await _db.PlatformUsers
            .Include(u => u.Salons)
            .FirstOrDefaultAsync(u => u.Phone == dto.Phone);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Telefon veya şifre hatalı." });

        if (!user.IsActive)
            return Unauthorized(new { message = "Hesabınız devre dışı bırakılmış." });

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _tokenService.GeneratePlatformToken(user);
        return Ok(new PlatformAuthResponse
        {
            Token = token,
            User = MapToDto(user)
        });
    }

    /// <summary>Mevcut kullanici bilgileri</summary>
    [HttpGet("me")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult<PlatformUserDto>> GetMe()
    {
        var userId = GetPlatformUserId();
        var user = await _db.PlatformUsers
            .Include(u => u.Salons)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();
        return Ok(MapToDto(user));
    }

    /// <summary>Profil guncelle</summary>
    [HttpPut("me")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult> UpdateMe([FromBody] PlatformUserUpdateDto dto)
    {
        var userId = GetPlatformUserId();
        var user = await _db.PlatformUsers.FindAsync(userId);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName.Trim();
        if (dto.Email != null) user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(user));
    }

    private int GetPlatformUserId()
        => int.Parse(User.FindFirstValue("PlatformUserId") ?? "0");

    private static PlatformUserDto MapToDto(PlatformUser u) => new()
    {
        Uid = u.Uid,
        FullName = u.FullName,
        Phone = u.Phone,
        Email = u.Email,
        AvatarUrl = u.AvatarUrl,
        SalonCount = u.Salons?.Count(s => s.IsActive) ?? 0
    };
}

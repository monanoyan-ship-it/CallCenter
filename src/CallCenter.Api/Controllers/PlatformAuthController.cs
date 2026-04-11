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

        var normalizedPhone = CallCenter.Shared.Helpers.PhoneHelper.Normalize(dto.Phone) ?? "";
        if (string.IsNullOrEmpty(normalizedPhone))
            return BadRequest(new { message = "Geçerli bir telefon numarası giriniz." });

        var exists = await _db.PlatformUsers.AnyAsync(u => u.Phone == normalizedPhone);
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
            Phone = normalizedPhone,
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
        var normalizedPhone = CallCenter.Shared.Helpers.PhoneHelper.Normalize(dto.Phone) ?? "";
        var user = await _db.PlatformUsers
            .Include(u => u.Salons)
            .FirstOrDefaultAsync(u => u.Phone == normalizedPhone);

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

    /// <summary>Fatura bilgilerini guncelle</summary>
    [HttpPut("billing-info")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult> UpdateBillingInfo([FromBody] PlatformBillingUpdateDto dto)
    {
        var userId = GetPlatformUserId();
        var user = await _db.PlatformUsers.FindAsync(userId);
        if (user == null) return NotFound();

        user.BillingType = dto.BillingType;
        user.BillingFullName = dto.BillingFullName;
        user.BillingCompanyName = dto.BillingCompanyName;
        user.BillingTaxOffice = dto.BillingTaxOffice;
        user.BillingTaxNumber = dto.BillingTaxNumber;
        user.BillingAddress = dto.BillingAddress;
        user.BillingCity = dto.BillingCity;
        user.BillingDistrict = dto.BillingDistrict;
        user.BillingPostalCode = dto.BillingPostalCode;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(user));
    }

    private static PlatformUserDto MapToDto(PlatformUser u) => new()
    {
        Uid = u.Uid,
        FullName = u.FullName,
        Phone = u.Phone,
        Email = u.Email,
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

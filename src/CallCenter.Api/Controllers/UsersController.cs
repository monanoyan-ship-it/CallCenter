using System.Security.Claims;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Sayfalamali kullanici listesi</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? roleId = null)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(s)
                                  || u.UserName.ToLower().Contains(s)
                                  || u.Email.ToLower().Contains(s));
        }

        if (roleId.HasValue && roleId.Value > 0)
        {
            query = query.Where(u => u.RoleId == roleId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                RoleId = u.RoleId,
                Extension = u.Extension,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();

        // RoleName'i client-side doldur (TypeItem static lookup)
        foreach (var item in items)
        {
            item.RoleName = UserRoles.GetById(item.RoleId)?.SystemName ?? "Unknown";
        }

        return Ok(new PagedResult<UserListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>Kullanici detay</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserListDto>> GetById(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound(new { message = "Kullanici bulunamadi." });

        return Ok(new UserListDto
        {
            Id = u.Id,
            UserName = u.UserName,
            FullName = u.FullName,
            Email = u.Email,
            RoleId = u.RoleId,
            RoleName = UserRoles.GetById(u.RoleId)?.SystemName ?? "Unknown",
            Extension = u.Extension,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        });
    }

    /// <summary>Yeni kullanici olustur</summary>
    [HttpPost]
    public async Task<ActionResult> Create(UserCreateDto dto)
    {
        // Unique kontrol
        if (await _db.Users.AnyAsync(u => u.UserName == dto.UserName))
            return BadRequest(new { message = "Bu kullanici adi zaten kullaniliyor." });

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Bu e-posta adresi zaten kullaniliyor." });

        // Rol gecerlilik kontrolu
        if (UserRoles.GetById(dto.RoleId) == null)
            return BadRequest(new { message = "Gecersiz rol." });

        var user = new Shared.Entities.User
        {
            UserName = dto.UserName,
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = dto.RoleId,
            Extension = dto.Extension,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { id = user.Id });
    }

    /// <summary>Kullanici guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UserUpdateDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "Kullanici bulunamadi." });

        // Email unique kontrolu (kendisi haric)
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
            return BadRequest(new { message = "Bu e-posta adresi zaten kullaniliyor." });

        // Rol gecerlilik kontrolu
        if (UserRoles.GetById(dto.RoleId) == null)
            return BadRequest(new { message = "Gecersiz rol." });

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.RoleId = dto.RoleId;
        user.Extension = dto.Extension;
        user.IsActive = dto.IsActive;

        // Sifre opsiyonel — sadece dolu ise guncelle
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Kullanici sil (soft delete — IsActive=false)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        // Admin kendini silemez
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(currentUserIdStr, out var currentUserId) && currentUserId == id)
            return BadRequest(new { message = "Kendinizi silemezsiniz." });

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "Kullanici bulunamadi." });

        user.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

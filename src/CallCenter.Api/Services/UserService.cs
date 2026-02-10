using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<UserListDto>> GetAllAsync(int page, int pageSize, string? search, int? roleId)
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

        foreach (var item in items)
        {
            item.RoleName = UserRoles.GetById(item.RoleId)?.SystemName ?? "Unknown";
        }

        return new PagedResult<UserListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserListDto?> GetByIdAsync(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return null;

        return new UserListDto
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
        };
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateAsync(UserCreateDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.UserName == dto.UserName))
            return (false, null, "Bu kullanici adi zaten kullaniliyor.");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return (false, null, "Bu e-posta adresi zaten kullaniliyor.");

        if (UserRoles.GetById(dto.RoleId) == null)
            return (false, null, "Gecersiz rol.");

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

        return (true, user.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UserUpdateDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return (false, "Kullanici bulunamadi.");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
            return (false, "Bu e-posta adresi zaten kullaniliyor.");

        if (UserRoles.GetById(dto.RoleId) == null)
            return (false, "Gecersiz rol.");

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.RoleId = dto.RoleId;
        user.Extension = dto.Extension;
        user.IsActive = dto.IsActive;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, int currentUserId)
    {
        if (currentUserId == id)
            return (false, "Kendinizi silemezsiniz.");

        var user = await _db.Users.FindAsync(id);
        if (user == null) return (false, "Kullanici bulunamadi.");

        user.IsActive = false;
        await _db.SaveChangesAsync();

        return (true, null);
    }
}

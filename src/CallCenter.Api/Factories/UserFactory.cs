using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class UserFactory : IUserFactory
{
    private readonly IUserEntityService _users;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly IPasswordPolicyFactory _passwordPolicy;
    private readonly IUnitOfWork _uow;

    public UserFactory(IUserEntityService users, ICustomerPersonnelEntityService personnel, IPasswordPolicyFactory passwordPolicy, IUnitOfWork uow)
    {
        _users = users;
        _personnel = personnel;
        _passwordPolicy = passwordPolicy;
        _uow = uow;
    }

    public async Task<PagedResult<UserListDto>> GetAllAsync(int page, int pageSize, string? search, int? roleId)
    {
        var query = _users.GetAllQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(s)
                                  || u.UserName.ToLower().Contains(s)
                                  || u.Email.ToLower().Contains(s));
        }

        if (roleId.HasValue && roleId.Value > 0)
            query = query.Where(u => u.RoleId == roleId.Value);

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

        // CustomerPersonnel bilgilerini batch olarak getir
        var userIds = items.Select(i => i.Id).ToList();
        var personnelMap = await _personnel.GetAllQueryable()
            .Where(cp => userIds.Contains(cp.UserId))
            .Select(cp => new { cp.UserId, cp.CustomerRoleId, CustomerName = cp.Customer.Name })
            .ToDictionaryAsync(x => x.UserId);

        foreach (var item in items)
        {
            item.RoleName = UserRoles.GetById(item.RoleId)?.SystemName ?? "Unknown";
            if (personnelMap.TryGetValue(item.Id, out var cp))
            {
                item.CustomerRoleId = cp.CustomerRoleId;
                item.CustomerRoleName = CustomerRoles.GetById(cp.CustomerRoleId)?.Description;
                item.CustomerName = cp.CustomerName;
            }
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
        var u = await _users.GetByIdAsync(id);
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
        if (await _users.ExistsByUsernameAsync(dto.UserName))
            return (false, null, "Bu kullanici adi zaten kullaniliyor.");

        if (await _users.ExistsByEmailAsync(dto.Email))
            return (false, null, "Bu e-posta adresi zaten kullaniliyor.");

        if (UserRoles.GetById(dto.RoleId) == null)
            return (false, null, "Gecersiz rol.");

        var (isValid, errors) = _passwordPolicy.ValidatePassword(dto.Password);
        if (!isValid)
            return (false, null, string.Join(" ", errors));

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new Shared.Entities.User
        {
            UserName = dto.UserName,
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = passwordHash,
            RoleId = dto.RoleId,
            Extension = dto.Extension,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PasswordChangedAt = DateTime.UtcNow
        };

        _users.Add(user);
        await _uow.SaveChangesAsync();

        await _passwordPolicy.RecordPasswordAsync(user.Id, passwordHash);

        return (true, user.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UserUpdateDto dto)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null) return (false, "Kullanici bulunamadi.");

        if (await _users.ExistsByEmailAsync(dto.Email, id))
            return (false, "Bu e-posta adresi zaten kullaniliyor.");

        if (UserRoles.GetById(dto.RoleId) == null)
            return (false, "Gecersiz rol.");

        if (user.RoleId == UserRoles.Ids.Admin && (dto.RoleId != UserRoles.Ids.Admin || !dto.IsActive))
        {
            var activeAdminCount = await _users.GetActiveAdminCountAsync();
            if (activeAdminCount <= 1)
                return (false, "Sistemde en az bir admin olmalidir. Son adminin rolu degistirilemez veya deaktive edilemez.");
        }

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.RoleId = dto.RoleId;
        user.Extension = dto.Extension;
        user.IsActive = dto.IsActive;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var (isValid, errors) = _passwordPolicy.ValidatePassword(dto.Password);
            if (!isValid)
                return (false, string.Join(" ", errors));

            if (await _passwordPolicy.IsPasswordReusedAsync(id, dto.Password))
                return (false, "Bu şifre daha önce kullanılmış. Farklı bir şifre seçiniz.");

            var newHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.PasswordHash = newHash;
            user.PasswordChangedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();
            await _passwordPolicy.RecordPasswordAsync(id, newHash);
            return (true, null);
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, int currentUserId)
    {
        if (currentUserId == id)
            return (false, "Kendinizi silemezsiniz.");

        var user = await _users.GetByIdAsync(id);
        if (user == null) return (false, "Kullanici bulunamadi.");

        if (user.RoleId == UserRoles.Ids.Admin)
        {
            var activeAdminCount = await _users.GetActiveAdminCountAsync();
            if (activeAdminCount <= 1)
                return (false, "Sistemde en az bir admin olmalidir. Son admin silinemez.");
        }

        user.IsActive = false;
        await _uow.SaveChangesAsync();

        return (true, null);
    }
}

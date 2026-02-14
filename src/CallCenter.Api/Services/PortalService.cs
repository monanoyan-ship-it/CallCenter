using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class PortalService : IPortalService
{
    private readonly AppDbContext _db;
    private readonly AesEncryptionService _encryption;
    private readonly IPasswordPolicyService _passwordPolicy;

    public PortalService(AppDbContext db, AesEncryptionService encryption, IPasswordPolicyService passwordPolicy)
    {
        _db = db;
        _encryption = encryption;
        _passwordPolicy = passwordPolicy;
    }

    // ═══════════════════════════════════════════════════════════════
    // DASHBOARD
    // ═══════════════════════════════════════════════════════════════

    public async Task<PortalDashboardDto> GetDashboardAsync(int customerId)
    {
        var today = DateTime.UtcNow.Date;

        var personnelCount = await _db.CustomerPersonnel
            .CountAsync(p => p.CustomerId == customerId && p.IsActive);

        var userTypeCount = await _db.CustomerUserTypes
            .CountAsync(ut => ut.CustomerId == customerId && ut.IsActive);

        var activeModules = await _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .ToListAsync();

        var sipCount = await _db.SipAccounts
            .CountAsync(s => s.CustomerId == customerId && s.IsActive);

        // Bugunku aramalar — CustomerPersonnel → User → CallRecord
        var personnelUserIds = await _db.CustomerPersonnel
            .Where(p => p.CustomerId == customerId)
            .Select(p => p.UserId)
            .ToListAsync();

        var callsToday = personnelUserIds.Count > 0
            ? await _db.CallRecords
                .CountAsync(c => personnelUserIds.Contains(c.AgentId ?? 0) && c.StartedAt >= today)
            : 0;

        var modules = PortalModules.All.Select(m => new PortalModuleSummaryDto
        {
            ModuleId = m.Id,
            ModuleName = m.SystemName,
            Icon = m.Icon,
            IsActive = activeModules.Any(am => am.ModuleId == m.Id)
        }).ToList();

        return new PortalDashboardDto
        {
            PersonnelCount = personnelCount,
            ActiveModuleCount = activeModules.Count,
            UserTypeCount = userTypeCount,
            TotalCallsToday = callsToday,
            SipAccountCount = sipCount,
            Modules = modules
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // USERTYPES
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<UserTypeListDto>> GetUserTypesAsync(int customerId)
    {
        return await _db.CustomerUserTypes
            .Where(ut => ut.CustomerId == customerId)
            .Select(ut => new UserTypeListDto
            {
                Id = ut.Id,
                Uid = ut.Uid,
                Name = ut.Name,
                Description = ut.Description,
                Level = ut.Level,
                CanManageSubordinates = ut.CanManageSubordinates,
                IsActive = ut.IsActive,
                PersonnelCount = ut.Personnel.Count(p => p.IsActive),
                PermissionCount = ut.Permissions.Count
            })
            .OrderBy(ut => ut.Name)
            .ToListAsync();
    }

    public async Task<(bool Success, object Result)> CreateUserTypeAsync(int customerId, UserTypeCreateDto dto)
    {
        var exists = await _db.CustomerUserTypes
            .AnyAsync(ut => ut.CustomerId == customerId && ut.Name == dto.Name);
        if (exists)
            return (false, "Bu isimde bir kullanici tipi zaten var.");

        var entity = new CustomerUserType
        {
            CustomerId = customerId,
            Name = dto.Name,
            Description = dto.Description,
            Level = dto.Level,
            CanManageSubordinates = dto.CanManageSubordinates
        };

        _db.CustomerUserTypes.Add(entity);
        await _db.SaveChangesAsync();

        return (true, new UserTypeListDto
        {
            Id = entity.Id,
            Uid = entity.Uid,
            Name = entity.Name,
            Description = entity.Description,
            Level = entity.Level,
            CanManageSubordinates = entity.CanManageSubordinates,
            IsActive = entity.IsActive,
            PersonnelCount = 0,
            PermissionCount = 0
        });
    }

    public async Task<(bool Success, string? Error)> UpdateUserTypeAsync(int customerId, int id, UserTypeUpdateDto dto)
    {
        var entity = await _db.CustomerUserTypes
            .FirstOrDefaultAsync(ut => ut.Id == id && ut.CustomerId == customerId);
        if (entity == null)
            return (false, "Kullanici tipi bulunamadi.");

        // Varsayilan (seed) Yonetici tipi koruması:
        // Musterinin ilk olusturulan UserType'i degistirilemez ve deaktif edilemez
        var isDefault = await IsDefaultUserTypeAsync(customerId, id);
        if (isDefault)
        {
            // Ad, aktiflik ve seviye degistirilemez
            if (dto.Name != entity.Name)
                return (false, "Varsayilan Yonetici tipinin adi degistirilemez.");
            if (!dto.IsActive)
                return (false, "Varsayilan Yonetici tipi deaktif edilemez.");
            if (dto.Level != entity.Level)
                return (false, "Varsayilan Yonetici tipinin seviyesi degistirilemez.");
        }

        // Ayni isimde baska bir tip var mi?
        var duplicate = await _db.CustomerUserTypes
            .AnyAsync(ut => ut.CustomerId == customerId && ut.Name == dto.Name && ut.Id != id);
        if (duplicate)
            return (false, "Bu isimde bir kullanici tipi zaten var.");

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Level = dto.Level;
        entity.CanManageSubordinates = dto.CanManageSubordinates;
        entity.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeactivateUserTypeAsync(int customerId, int id)
    {
        var entity = await _db.CustomerUserTypes
            .FirstOrDefaultAsync(ut => ut.Id == id && ut.CustomerId == customerId);
        if (entity == null)
            return (false, "Kullanici tipi bulunamadi.");

        // Varsayilan Yonetici tipi deaktif edilemez
        if (await IsDefaultUserTypeAsync(customerId, id))
            return (false, "Varsayilan Yonetici tipi deaktif edilemez.");

        entity.IsActive = false;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    /// <summary>
    /// Musterinin ilk olusturulan (seed) UserType'ini tespit eder.
    /// CreateCustomerAdminAsync her zaman ilk UserType'i olusturur, bu "Yonetici" tipidir.
    /// </summary>
    private async Task<bool> IsDefaultUserTypeAsync(int customerId, int userTypeId)
    {
        var firstTypeId = await _db.CustomerUserTypes
            .Where(ut => ut.CustomerId == customerId)
            .OrderBy(ut => ut.Id)
            .Select(ut => ut.Id)
            .FirstOrDefaultAsync();

        return firstTypeId == userTypeId;
    }

    public async Task<int[]> GetUserTypePermissionsAsync(int customerId, int id)
    {
        return await _db.CustomerUserTypePermissions
            .Where(p => p.UserType.CustomerId == customerId && p.UserTypeId == id)
            .Select(p => p.PermissionTypeId)
            .ToArrayAsync();
    }

    public async Task<(bool Success, string? Error)> SetUserTypePermissionsAsync(int customerId, int id, int[] permissionTypeIds)
    {
        var entity = await _db.CustomerUserTypes
            .Include(ut => ut.Permissions)
            .FirstOrDefaultAsync(ut => ut.Id == id && ut.CustomerId == customerId);
        if (entity == null)
            return (false, "Kullanici tipi bulunamadi.");

        // Musterinin acik modulleri
        var activeModuleIds = await _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .Select(m => m.ModuleId)
            .ToListAsync();

        // Sadece acik modullerdeki yetkilere izin ver
        var validPermIds = permissionTypeIds
            .Where(pid => activeModuleIds.Contains(CustomerPermissionTypes.GetModuleId(pid)))
            .Distinct()
            .ToArray();

        // Mevcut yetkileri temizle, yenilerini ekle
        entity.Permissions.Clear();
        foreach (var pid in validPermIds)
        {
            entity.Permissions.Add(new CustomerUserTypePermission
            {
                UserTypeId = entity.Id,
                PermissionTypeId = pid
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════════
    // PERSONNEL
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<PortalPersonnelListDto>> GetPersonnelAsync(int customerId)
    {
        return await _db.CustomerPersonnel
            .Where(p => p.CustomerId == customerId)
            .Include(p => p.User)
            .Include(p => p.UserType)
            .Select(p => new PortalPersonnelListDto
            {
                Id = p.Id,
                UserName = p.User.UserName,
                FullName = p.User.FullName,
                Email = p.User.Email,
                Title = p.Title,
                UserTypeId = p.UserTypeId,
                UserTypeName = p.UserType != null ? p.UserType.Name : null,
                OrganizationUnitId = p.OrganizationUnitId,
                OrganizationUnitName = p.OrganizationUnit != null ? p.OrganizationUnit.Name : null,
                ReportsToPersonnelId = p.ReportsToPersonnelId,
                ReportsToPersonnelName = p.ReportsToPersonnel != null ? p.ReportsToPersonnel.User.FullName : null,
                IsActive = p.IsActive && p.User.IsActive,
                PermissionCount = p.Permissions.Count(pp => pp.IsActive)
            })
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public async Task<(bool Success, object Result)> CreatePersonnelAsync(int customerId, PortalPersonnelCreateDto dto, int createdByUserId)
    {
        // UserName unique kontrol
        var userNameExists = await _db.Users.AnyAsync(u => u.UserName == dto.UserName);
        if (userNameExists)
            return (false, "Bu kullanici adi zaten kullaniliyor.");

        // Email unique kontrol
        var emailExists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
        if (emailExists)
            return (false, "Bu e-posta adresi zaten kullaniliyor.");

        // Sifre politikasi kontrolu
        var (isValid, errors) = _passwordPolicy.ValidatePassword(dto.Password);
        if (!isValid)
            return (false, string.Join(" ", errors));

        // User olustur
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = new User
        {
            UserName = dto.UserName,
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = passwordHash,
            RoleId = UserRoles.Ids.CustomerUser,
            StatusId = AgentStatuses.Ids.Offline,
            IsActive = true,
            PasswordChangedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Sifre gecmisine kaydet
        await _passwordPolicy.RecordPasswordAsync(user.Id, passwordHash);

        // CustomerPersonnel olustur
        var personnel = new CustomerPersonnel
        {
            UserId = user.Id,
            CustomerId = customerId,
            Title = dto.Title,
            UserTypeId = dto.UserTypeId,
            OrganizationUnitId = dto.OrganizationUnitId,
            ReportsToPersonnelId = dto.ReportsToPersonnelId,
            IsActive = true
        };
        _db.CustomerPersonnel.Add(personnel);
        await _db.SaveChangesAsync();

        // UserType secildiyse, template yetkilerini personele kopyala
        if (dto.UserTypeId.HasValue)
        {
            await CopyUserTypePermissionsToPersonnel(dto.UserTypeId.Value, personnel.Id, createdByUserId);
        }

        return (true, new PortalPersonnelListDto
        {
            Id = personnel.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Title = personnel.Title,
            UserTypeId = personnel.UserTypeId,
            IsActive = true,
            PermissionCount = 0
        });
    }

    public async Task<(bool Success, string? Error)> UpdatePersonnelAsync(int customerId, int id, PortalPersonnelUpdateDto dto)
    {
        var personnel = await _db.CustomerPersonnel
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        if (personnel == null)
            return (false, "Personel bulunamadi.");

        // UserName unique kontrol (degistiyse)
        if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != personnel.User.UserName)
        {
            var userNameExists = await _db.Users
                .AnyAsync(u => u.UserName == dto.UserName && u.Id != personnel.UserId);
            if (userNameExists)
                return (false, "Bu kullanici adi zaten kullaniliyor.");

            personnel.User.UserName = dto.UserName;
        }

        // Email unique kontrol
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == dto.Email && u.Id != personnel.UserId);
        if (emailExists)
            return (false, "Bu e-posta adresi zaten kullaniliyor.");

        // Update ile deaktive edilmeye calisiliyorsa son admin kontrolu
        if (!dto.IsActive && personnel.IsActive && personnel.IsCustomerAdmin)
        {
            var activeAdminCount = await _db.CustomerPersonnel
                .CountAsync(p => p.CustomerId == customerId && p.IsCustomerAdmin && p.IsActive);
            if (activeAdminCount <= 1)
                return (false, "Firmada en az bir yonetici olmalidir. Son yonetici deaktive edilemez.");
        }

        personnel.User.FullName = dto.FullName;
        personnel.User.Email = dto.Email;
        personnel.User.IsActive = dto.IsActive;
        personnel.IsActive = dto.IsActive;

        if (!string.IsNullOrWhiteSpace(dto.Title))
            personnel.Title = dto.Title;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var (pwValid, pwErrors) = _passwordPolicy.ValidatePassword(dto.Password);
            if (!pwValid)
                return (false, string.Join(" ", pwErrors));

            if (await _passwordPolicy.IsPasswordReusedAsync(personnel.UserId, dto.Password))
                return (false, "Bu şifre daha önce kullanılmış. Farklı bir şifre seçiniz.");

            var newHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            personnel.User.PasswordHash = newHash;
            personnel.User.PasswordChangedAt = DateTime.UtcNow;

            // SaveChanges sonrasi gecmise kaydet (asagida)
            await _db.SaveChangesAsync();
            await _passwordPolicy.RecordPasswordAsync(personnel.UserId, newHash);
        }

        // UserType degistiyse guncelle
        if (dto.UserTypeId != personnel.UserTypeId)
        {
            personnel.UserTypeId = dto.UserTypeId;
        }

        personnel.OrganizationUnitId = dto.OrganizationUnitId;
        personnel.ReportsToPersonnelId = dto.ReportsToPersonnelId;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeactivatePersonnelAsync(int customerId, int id)
    {
        var personnel = await _db.CustomerPersonnel
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        if (personnel == null)
            return (false, "Personel bulunamadi.");

        // Son customer admin koruması — firmada en az 1 admin kalmali
        if (personnel.IsCustomerAdmin)
        {
            var activeAdminCount = await _db.CustomerPersonnel
                .CountAsync(p => p.CustomerId == customerId && p.IsCustomerAdmin && p.IsActive);
            if (activeAdminCount <= 1)
                return (false, "Firmada en az bir yonetici olmalidir. Son yonetici deaktive edilemez.");
        }

        personnel.IsActive = false;
        personnel.User.IsActive = false;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<List<PersonnelPermissionDto>> GetPersonnelPermissionsAsync(int customerId, int personnelId)
    {
        var personnel = await _db.CustomerPersonnel
            .FirstOrDefaultAsync(p => p.Id == personnelId && p.CustomerId == customerId);
        if (personnel == null)
            return new List<PersonnelPermissionDto>();

        var rawPerms = await _db.CustomerPersonnelPermissions
            .Where(p => p.PersonnelId == personnelId)
            .ToListAsync();

        return rawPerms.Select(p =>
        {
            var permType = CustomerPermissionTypes.GetById(p.PermissionTypeId);
            var scope = PermissionScopes.GetById(p.ScopeId);
            var moduleId = CustomerPermissionTypes.GetModuleId(p.PermissionTypeId);
            var module = PortalModules.GetById(moduleId);

            return new PersonnelPermissionDto
            {
                Id = p.Id,
                PermissionTypeId = p.PermissionTypeId,
                PermissionName = permType?.SystemName ?? "",
                PermissionDescription = permType?.Description,
                PermissionIcon = permType?.Icon,
                ScopeId = p.ScopeId,
                ScopeName = scope?.SystemName,
                IsActive = p.IsActive,
                ValidFrom = p.ValidFrom,
                ValidUntil = p.ValidUntil,
                Description = p.Description,
                ModuleId = moduleId,
                ModuleName = module?.SystemName
            };
        }).ToList();
    }

    public async Task<(bool Success, string? Error)> SetPersonnelPermissionsAsync(int customerId, int personnelId, int[] permissionTypeIds, int scopeId, int userId)
    {
        var personnel = await _db.CustomerPersonnel
            .FirstOrDefaultAsync(p => p.Id == personnelId && p.CustomerId == customerId);
        if (personnel == null)
            return (false, "Personel bulunamadi.");

        // Musterinin acik modulleri
        var activeModuleIds = await _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .Select(m => m.ModuleId)
            .ToListAsync();

        var validPermIds = permissionTypeIds
            .Where(pid => activeModuleIds.Contains(CustomerPermissionTypes.GetModuleId(pid)))
            .Distinct()
            .ToArray();

        // Mevcut yetkileri sil
        var existingPerms = await _db.CustomerPersonnelPermissions
            .Where(p => p.PersonnelId == personnelId)
            .ToListAsync();
        _db.CustomerPersonnelPermissions.RemoveRange(existingPerms);

        // Yeni yetkileri ekle
        foreach (var pid in validPermIds)
        {
            _db.CustomerPersonnelPermissions.Add(new CustomerPersonnelPermission
            {
                PersonnelId = personnelId,
                PermissionTypeId = pid,
                ScopeId = scopeId,
                IsActive = true,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════════
    // MODULES
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<PortalModuleDto>> GetModulesAsync(int customerId)
    {
        var activeModules = await _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId)
            .ToListAsync();

        return PortalModules.All.Select(m =>
        {
            var isActive = activeModules.Any(am => am.ModuleId == m.Id && am.IsActive);
            var permissions = CustomerPermissionTypes.GetByModule(m.Id).Select(p => new PermissionTypeDto
            {
                Id = p.Id,
                SystemName = p.SystemName,
                Description = p.Description,
                Icon = p.Icon,
                ModuleId = m.Id,
                ModuleName = m.SystemName
            }).ToList();

            return new PortalModuleDto
            {
                Id = m.Id,
                SystemName = m.SystemName,
                Description = m.Description,
                Icon = m.Icon,
                IsActive = isActive,
                Permissions = permissions
            };
        }).ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // SIP
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<PortalSipAccountDto>> GetSipAccountsAsync(int customerId)
    {
        return await _db.SipAccounts
            .Where(s => s.CustomerId == customerId)
            .Select(s => new PortalSipAccountDto
            {
                Id = s.Id,
                Name = s.Name,
                Server = s.Server,
                Port = s.Port,
                Username = s.Username,
                Transport = s.Transport,
                IsDefault = s.IsDefault,
                IsActive = s.IsActive
            })
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateSipAccountAsync(int customerId, int id, PortalSipUpdateDto dto)
    {
        var account = await _db.SipAccounts
            .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customerId);
        if (account == null)
            return (false, "SIP hesabi bulunamadi.");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            account.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Username))
            account.Username = dto.Username;
        if (!string.IsNullOrWhiteSpace(dto.Password))
            account.Password = _encryption.Encrypt(dto.Password);

        if (dto.IsDefault == true)
        {
            // Diger hesaplarin IsDefault'unu kapat
            var others = await _db.SipAccounts
                .Where(s => s.CustomerId == customerId && s.Id != id && s.IsDefault)
                .ToListAsync();
            foreach (var o in others) o.IsDefault = false;
            account.IsDefault = true;
        }
        else if (dto.IsDefault == false)
        {
            account.IsDefault = false;
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════

    private async Task CopyUserTypePermissionsToPersonnel(int userTypeId, int personnelId, int createdByUserId)
    {
        var templatePerms = await _db.CustomerUserTypePermissions
            .Where(p => p.UserTypeId == userTypeId)
            .Select(p => p.PermissionTypeId)
            .ToListAsync();

        foreach (var pid in templatePerms)
        {
            _db.CustomerPersonnelPermissions.Add(new CustomerPersonnelPermission
            {
                PersonnelId = personnelId,
                PermissionTypeId = pid,
                ScopeId = PermissionScopes.Ids.Customer,
                IsActive = true,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (templatePerms.Count > 0)
            await _db.SaveChangesAsync();
    }
}

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

        var customer = await _db.Customers.FindAsync(customerId);

        var personnelCount = await _db.CustomerPersonnel
            .CountAsync(p => p.CustomerId == customerId && p.IsActive);

        var callableUserCount = await _db.CustomerPersonnel
            .CountAsync(p => p.CustomerId == customerId && p.IsActive
                && p.CustomerRoleId != CustomerRoles.Ids.FirmaAdmin);

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
            MaxUsers = customer?.MaxUsers ?? 0,
            CallableUserCount = callableUserCount,
            TotalCallsToday = callsToday,
            SipAccountCount = sipCount,
            Modules = modules
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // PERSONNEL
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<PortalPersonnelListDto>> GetPersonnelAsync(int customerId)
    {
        var personnel = await _db.CustomerPersonnel
            .Where(p => p.CustomerId == customerId)
            .Include(p => p.User)
            .Select(p => new PortalPersonnelListDto
            {
                Id = p.Id,
                UserName = p.User.UserName,
                FullName = p.User.FullName,
                Email = p.User.Email,
                Title = p.Title,
                CustomerRoleId = p.CustomerRoleId,
                OrganizationUnitId = p.OrganizationUnitId,
                OrganizationUnitName = p.OrganizationUnit != null ? p.OrganizationUnit.Name : null,
                ReportsToPersonnelId = p.ReportsToPersonnelId,
                ReportsToPersonnelName = p.ReportsToPersonnel != null ? p.ReportsToPersonnel.User.FullName : null,
                IsActive = p.IsActive && p.User.IsActive,
                PermissionCount = p.Permissions.Count(pp => pp.IsActive)
            })
            .OrderBy(p => p.FullName)
            .ToListAsync();

        // CustomerRoleName'i memory'de ata (TypeItem DB'de degil)
        foreach (var p in personnel)
            p.CustomerRoleName = CustomerRoles.GetById(p.CustomerRoleId)?.Description;

        return personnel;
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

        // MaxUsers limit kontrolu (FirmaAdmin haric — aranabilir roller)
        if (dto.CustomerRoleId != CustomerRoles.Ids.FirmaAdmin)
        {
            var customer = await _db.Customers.FindAsync(customerId);
            if (customer?.MaxUsers > 0)
            {
                var callableCount = await _db.CustomerPersonnel
                    .CountAsync(p => p.CustomerId == customerId && p.IsActive
                        && p.CustomerRoleId != CustomerRoles.Ids.FirmaAdmin);
                if (callableCount >= customer.MaxUsers)
                    return (false, $"Maksimum kullanici limitine ({customer.MaxUsers}) ulasildi.");
            }
        }

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

        // IsCustomerAdmin auto-set: FirmaAdmin rolu secildiyse
        var isAdmin = dto.CustomerRoleId == CustomerRoles.Ids.FirmaAdmin;

        // CustomerPersonnel olustur
        var personnel = new CustomerPersonnel
        {
            UserId = user.Id,
            CustomerId = customerId,
            Title = dto.Title,
            CustomerRoleId = dto.CustomerRoleId,
            IsCustomerAdmin = isAdmin,
            OrganizationUnitId = dto.OrganizationUnitId,
            ReportsToPersonnelId = dto.ReportsToPersonnelId,
            IsActive = true
        };
        _db.CustomerPersonnel.Add(personnel);
        await _db.SaveChangesAsync();

        return (true, new PortalPersonnelListDto
        {
            Id = personnel.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Title = personnel.Title,
            CustomerRoleId = personnel.CustomerRoleId,
            CustomerRoleName = CustomerRoles.GetById(personnel.CustomerRoleId)?.Description,
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

        // Rol degistiyse guncelle + IsCustomerAdmin sync
        if (dto.CustomerRoleId != personnel.CustomerRoleId)
        {
            personnel.CustomerRoleId = dto.CustomerRoleId;
            personnel.IsCustomerAdmin = dto.CustomerRoleId == CustomerRoles.Ids.FirmaAdmin;
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

        // Tum izinler her zaman kullanilabilir (modul filtreleme kaldirildi)
        var validPermIds = permissionTypeIds
            .Where(pid => CustomerPermissionTypes.GetById(pid) != null)
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
}

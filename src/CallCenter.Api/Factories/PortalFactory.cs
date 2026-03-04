using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class PortalFactory : IPortalFactory
{
    private readonly ICustomerEntityService _customerEs;
    private readonly ICustomerPersonnelEntityService _personnelEs;
    private readonly ICustomerPortalModuleEntityService _moduleEs;
    private readonly ICustomerPersonnelPermissionEntityService _permissionEs;
    private readonly ISipAccountEntityService _sipEs;
    private readonly IUserEntityService _userEs;
    private readonly ICallRecordEntityService _callEs;
    private readonly IPasswordPolicyFactory _passwordPolicy;
    private readonly AesEncryptionService _encryption;
    private readonly IUnitOfWork _uow;

    public PortalFactory(
        ICustomerEntityService customerEs,
        ICustomerPersonnelEntityService personnelEs,
        ICustomerPortalModuleEntityService moduleEs,
        ICustomerPersonnelPermissionEntityService permissionEs,
        ISipAccountEntityService sipEs,
        IUserEntityService userEs,
        ICallRecordEntityService callEs,
        IPasswordPolicyFactory passwordPolicy,
        AesEncryptionService encryption,
        IUnitOfWork uow)
    {
        _customerEs = customerEs;
        _personnelEs = personnelEs;
        _moduleEs = moduleEs;
        _permissionEs = permissionEs;
        _sipEs = sipEs;
        _userEs = userEs;
        _callEs = callEs;
        _passwordPolicy = passwordPolicy;
        _encryption = encryption;
        _uow = uow;
    }

    // DASHBOARD

    public async Task<PortalDashboardDto> GetDashboardAsync(int customerId, int? callerPersonnelId = null, int? callerRoleId = null)
    {
        var today = DateTime.UtcNow.Date;
        var isEkipLideri = callerRoleId == CustomerRoles.Ids.EkipLideri && callerPersonnelId.HasValue;

        List<int>? teamMemberIds = null;
        if (isEkipLideri)
        {
            teamMemberIds = await _personnelEs.GetTeamMemberIdsAsync(callerPersonnelId!.Value, customerId);
            teamMemberIds.Add(callerPersonnelId!.Value);
        }

        var customer = await _customerEs.GetByIdAsync(customerId);

        int personnelCount;
        int callableUserCount;
        if (isEkipLideri && teamMemberIds != null)
        {
            personnelCount = await _personnelEs.GetAllQueryable()
                .CountAsync(p => p.CustomerId == customerId && p.IsActive && teamMemberIds.Contains(p.Id));
            callableUserCount = await _personnelEs.GetAllQueryable()
                .CountAsync(p => p.CustomerId == customerId && p.IsActive && teamMemberIds.Contains(p.Id)
                    && p.CustomerRoleId != CustomerRoles.Ids.FirmaAdmin
                    && p.CustomerRoleId != CustomerRoles.Ids.EkipLideri);
        }
        else
        {
            personnelCount = await _personnelEs.GetActiveCountAsync(customerId);
            callableUserCount = await _personnelEs.GetActiveCountAsync(customerId, excludeAdmin: true);
        }

        var activeModules = await _moduleEs.GetAllQueryable()
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .ToListAsync();

        var sipCount = await _sipEs.GetAllQueryable()
            .CountAsync(s => s.CustomerId == customerId && s.IsActive);

        var personnelQuery = _personnelEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId);
        if (isEkipLideri && teamMemberIds != null)
            personnelQuery = personnelQuery.Where(p => teamMemberIds.Contains(p.Id));

        var personnelUserIds = await personnelQuery
            .Select(p => p.UserId)
            .ToListAsync();

        var callsToday = personnelUserIds.Count > 0
            ? await _callEs.GetAllQueryable()
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

    // PERSONNEL

    public async Task<List<PortalPersonnelListDto>> GetPersonnelAsync(int customerId, int? callerPersonnelId = null, int? callerRoleId = null)
    {
        var isEkipLideri = callerRoleId == CustomerRoles.Ids.EkipLideri && callerPersonnelId.HasValue;
        List<int>? teamMemberIds = null;
        if (isEkipLideri)
        {
            teamMemberIds = await _personnelEs.GetTeamMemberIdsAsync(callerPersonnelId!.Value, customerId);
            teamMemberIds.Add(callerPersonnelId!.Value);
        }

        var query = _personnelEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId);
        if (isEkipLideri && teamMemberIds != null)
            query = query.Where(p => teamMemberIds.Contains(p.Id));

        var personnel = await query
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

        foreach (var p in personnel)
            p.CustomerRoleName = CustomerRoles.GetById(p.CustomerRoleId)?.Description;

        return personnel;
    }

    public async Task<(bool Success, object Result)> CreatePersonnelAsync(int customerId, PortalPersonnelCreateDto dto, int createdByUserId)
    {
        if (await _userEs.ExistsByUsernameAsync(dto.UserName))
            return (false, "Bu kullanici adi zaten kullaniliyor.");

        if (await _userEs.ExistsByEmailAsync(dto.Email))
            return (false, "Bu e-posta adresi zaten kullaniliyor.");

        var (isValid, errors) = _passwordPolicy.ValidatePassword(dto.Password);
        if (!isValid)
            return (false, string.Join(" ", errors));

        if (dto.CustomerRoleId != CustomerRoles.Ids.FirmaAdmin
            && dto.CustomerRoleId != CustomerRoles.Ids.EkipLideri)
        {
            var customer = await _customerEs.GetByIdAsync(customerId);
            if (customer != null)
            {
                var callableCount = await _personnelEs.GetActiveCountAsync(customerId, excludeAdmin: true);
                if (callableCount >= customer.MaxUsers)
                    return (false, $"Maksimum kullanici limitine ({customer.MaxUsers}) ulasildi.");
            }
        }

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
        _userEs.Add(user);
        await _uow.SaveChangesAsync();

        await _passwordPolicy.RecordPasswordAsync(user.Id, passwordHash);

        var isAdmin = dto.CustomerRoleId == CustomerRoles.Ids.FirmaAdmin;

        var personnelEntity = new CustomerPersonnel
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
        _personnelEs.Add(personnelEntity);
        await _uow.SaveChangesAsync();

        return (true, new PortalPersonnelListDto
        {
            Id = personnelEntity.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Title = personnelEntity.Title,
            CustomerRoleId = personnelEntity.CustomerRoleId,
            CustomerRoleName = CustomerRoles.GetById(personnelEntity.CustomerRoleId)?.Description,
            IsActive = true,
            PermissionCount = 0
        });
    }

    public async Task<(bool Success, string? Error)> UpdatePersonnelAsync(int customerId, int id, PortalPersonnelUpdateDto dto)
    {
        var personnel = await _personnelEs.GetByIdWithUserAsync(id, customerId);
        if (personnel == null)
            return (false, "Personel bulunamadi.");

        if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != personnel.User.UserName)
        {
            var userNameExists = await _userEs.GetAllQueryable()
                .AnyAsync(u => u.UserName == dto.UserName && u.Id != personnel.UserId);
            if (userNameExists)
                return (false, "Bu kullanici adi zaten kullaniliyor.");

            personnel.User.UserName = dto.UserName;
        }

        var emailExists = await _userEs.GetAllQueryable()
            .AnyAsync(u => u.Email == dto.Email && u.Id != personnel.UserId);
        if (emailExists)
            return (false, "Bu e-posta adresi zaten kullaniliyor.");

        if (!dto.IsActive && personnel.IsActive && personnel.IsCustomerAdmin)
        {
            var activeAdminCount = await _personnelEs.GetActiveAdminCountAsync(customerId);
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

            await _uow.SaveChangesAsync();
            await _passwordPolicy.RecordPasswordAsync(personnel.UserId, newHash);
        }

        if (dto.CustomerRoleId != personnel.CustomerRoleId)
        {
            personnel.CustomerRoleId = dto.CustomerRoleId;
            personnel.IsCustomerAdmin = dto.CustomerRoleId == CustomerRoles.Ids.FirmaAdmin;
        }

        personnel.OrganizationUnitId = dto.OrganizationUnitId;
        personnel.ReportsToPersonnelId = dto.ReportsToPersonnelId;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeactivatePersonnelAsync(int customerId, int id)
    {
        var personnel = await _personnelEs.GetByIdWithUserAsync(id, customerId);
        if (personnel == null)
            return (false, "Personel bulunamadi.");

        if (personnel.IsCustomerAdmin)
        {
            var activeAdminCount = await _personnelEs.GetActiveAdminCountAsync(customerId);
            if (activeAdminCount <= 1)
                return (false, "Firmada en az bir yonetici olmalidir. Son yonetici deaktive edilemez.");
        }

        personnel.IsActive = false;
        personnel.User.IsActive = false;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<List<PersonnelPermissionDto>> GetPersonnelPermissionsAsync(int customerId, int personnelId)
    {
        var personnel = await _personnelEs.GetByIdAndCustomerAsync(personnelId, customerId);
        if (personnel == null)
            return new List<PersonnelPermissionDto>();

        var rawPerms = await _permissionEs.GetByPersonnelIdAsync(personnelId);

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
        var personnel = await _personnelEs.GetByIdAndCustomerAsync(personnelId, customerId);
        if (personnel == null)
            return (false, "Personel bulunamadi.");

        var validPermIds = permissionTypeIds
            .Where(pid => CustomerPermissionTypes.GetById(pid) != null)
            .Distinct()
            .ToArray();

        var existingPerms = await _permissionEs.GetByPersonnelIdAsync(personnelId);
        _permissionEs.RemoveRange(existingPerms);

        foreach (var pid in validPermIds)
        {
            _permissionEs.Add(new CustomerPersonnelPermission
            {
                PersonnelId = personnelId,
                PermissionTypeId = pid,
                ScopeId = scopeId,
                IsActive = true,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // MODULES

    public async Task<List<PortalModuleDto>> GetModulesAsync(int customerId)
    {
        var activeModules = await _moduleEs.GetAllQueryable()
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

    // SIP

    public async Task<List<PortalSipAccountDto>> GetSipAccountsAsync(int customerId)
    {
        return await _sipEs.GetAllQueryable()
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
        var account = await _sipEs.GetAllQueryable()
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
            var others = await _sipEs.GetAllQueryable()
                .Where(s => s.CustomerId == customerId && s.Id != id && s.IsDefault)
                .ToListAsync();
            foreach (var o in others) o.IsDefault = false;
            account.IsDefault = true;
        }
        else if (dto.IsDefault == false)
        {
            account.IsDefault = false;
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateSipAccountAsync(int customerId, PortalSipCreateDto dto)
    {
        var exists = await _sipEs.GetAllQueryable()
            .AnyAsync(s => s.CustomerId == customerId && s.Name == dto.Name);
        if (exists)
            return (false, null, "Bu isimde bir SIP hesabi zaten mevcut.");

        var account = new SipAccount
        {
            CustomerId = customerId,
            Name = dto.Name,
            Server = dto.Server,
            Port = dto.Port,
            Username = dto.Username,
            Password = _encryption.Encrypt(dto.Password),
            Transport = dto.Transport,
            IsDefault = dto.IsDefault,
            IsActive = true
        };

        if (dto.IsDefault)
        {
            var others = await _sipEs.GetAllQueryable()
                .Where(s => s.CustomerId == customerId && s.IsDefault)
                .ToListAsync();
            foreach (var o in others) o.IsDefault = false;
        }

        _sipEs.Add(account);
        await _uow.SaveChangesAsync();

        return (true, account.Id, null);
    }
}

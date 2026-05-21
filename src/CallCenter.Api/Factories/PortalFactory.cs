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
    private readonly ISipAccountEntityService _sipEs;
    private readonly IUserEntityService _userEs;
    private readonly ICallRecordEntityService _callEs;
    private readonly IPasswordPolicyFactory _passwordPolicy;
    private readonly AesEncryptionService _encryption;
    private readonly ISlnPersonnelSkillEntityService _skillEs;
    private readonly ISlnBranchEntityService _branchEs;
    private readonly ISlnServiceEntityService _serviceEs;
    private readonly ISlnPersonnelCommissionEntityService _commissionEs;
    private readonly ISlnPersonnelShiftEntityService _shiftEs;
    private readonly ISlnPersonnelLeaveEntityService _leaveEs;
    private readonly ISlnPersonnelTimesheetEntityService _timesheetEs;
    private readonly ISlnPayrollEntityService _payrollEs;
    private readonly ISlnAdvanceEntityService _advanceEs;
    private readonly ISlnInvoiceItemEntityService _invoiceItemEs;
    private readonly IUnitOfWork _uow;

    public PortalFactory(
        ICustomerEntityService customerEs,
        ICustomerPersonnelEntityService personnelEs,
        ICustomerPortalModuleEntityService moduleEs,
        ISipAccountEntityService sipEs,
        IUserEntityService userEs,
        ICallRecordEntityService callEs,
        IPasswordPolicyFactory passwordPolicy,
        AesEncryptionService encryption,
        ISlnPersonnelSkillEntityService skillEs,
        ISlnBranchEntityService branchEs,
        ISlnServiceEntityService serviceEs,
        ISlnPersonnelCommissionEntityService commissionEs,
        ISlnPersonnelShiftEntityService shiftEs,
        ISlnPersonnelLeaveEntityService leaveEs,
        ISlnPersonnelTimesheetEntityService timesheetEs,
        ISlnPayrollEntityService payrollEs,
        ISlnAdvanceEntityService advanceEs,
        ISlnInvoiceItemEntityService invoiceItemEs,
        IUnitOfWork uow)
    {
        _customerEs = customerEs;
        _personnelEs = personnelEs;
        _moduleEs = moduleEs;
        _sipEs = sipEs;
        _userEs = userEs;
        _callEs = callEs;
        _passwordPolicy = passwordPolicy;
        _encryption = encryption;
        _skillEs = skillEs;
        _branchEs = branchEs;
        _serviceEs = serviceEs;
        _commissionEs = commissionEs;
        _shiftEs = shiftEs;
        _leaveEs = leaveEs;
        _timesheetEs = timesheetEs;
        _payrollEs = payrollEs;
        _advanceEs = advanceEs;
        _invoiceItemEs = invoiceItemEs;
        _uow = uow;
    }

    // USERNAME CHECK

    public async Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null)
    {
        if (excludeUserId.HasValue)
            return !await _userEs.GetAllQueryable().AnyAsync(u => u.UserName == username && u.Id != excludeUserId.Value);

        return !await _userEs.ExistsByUsernameAsync(username);
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

    public async Task<List<PortalPersonnelListDto>> GetPersonnelAsync(int customerId, int? callerPersonnelId = null, int? callerRoleId = null, int? callerBranchId = null)
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
        var shouldApplyBranchScope = callerBranchId.HasValue
            && callerRoleId != SalonRoles.Ids.SalonOwner
            && callerRoleId != CustomerRoles.Ids.FirmaAdmin;
        if (shouldApplyBranchScope && callerBranchId is int scopedBranchId)
            query = query.Where(p => p.BranchId == scopedBranchId);

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
                BranchId = p.BranchId,
                BranchName = p.Branch != null ? p.Branch.Name : null,
                IsActive = p.IsActive && p.User.IsActive,
                IsLocked = p.User.LockedUntil.HasValue && p.User.LockedUntil.Value > DateTime.UtcNow,
                PhotoUrl = p.PhotoUrl,
                PublicVisible = p.PublicVisible,
                PublicShowFullName = p.PublicShowFullName,
                PublicShowPhoto = p.PublicShowPhoto,
                PublicShowTitle = p.PublicShowTitle,
                PublicShowSpecialty = p.PublicShowSpecialty,
                WorkingHoursJson = p.WorkingHoursJson
            })
            .OrderBy(p => p.FullName)
            .ToListAsync();

        // Skill bilgilerini yukle
        var personnelIds = personnel.Select(p => p.Id).ToList();
        var skills = await _skillEs.GetAllQueryable()
            .Where(s => personnelIds.Contains(s.PersonnelId))
            .ToListAsync();
        var skillMap = skills.GroupBy(s => s.PersonnelId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.ServiceId).ToList());

        foreach (var p in personnel)
        {
            p.CustomerRoleName = SalonRoles.GetById(p.CustomerRoleId)?.Description
                ?? CustomerRoles.GetById(p.CustomerRoleId)?.Description;
            p.SkillServiceIds = skillMap.GetValueOrDefault(p.Id);
        }

        return personnel;
    }

    public async Task<(bool Success, object Result)> CreatePersonnelAsync(int customerId, PortalPersonnelCreateDto dto, int createdByUserId)
    {
        if (dto.CustomerRoleId == SalonRoles.Ids.SalonOwner)
            return (false, "Salon Sahibi rolu atanamaz. Bu rol kayit sirasinda otomatik olusturulur.");

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

        var dependencyError = await ValidatePersonnelDependenciesAsync(
            customerId,
            dto.BranchId,
            dto.ReportsToPersonnelId,
            dto.SkillServiceIds);
        if (dependencyError != null)
            return (false, dependencyError);

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
            BranchId = dto.BranchId,
            IsActive = true,
            PublicVisible = dto.PublicVisible,
            PublicShowFullName = dto.PublicShowFullName,
            PublicShowPhoto = dto.PublicShowPhoto,
            PublicShowTitle = dto.PublicShowTitle,
            PublicShowSpecialty = dto.PublicShowSpecialty,
            WorkingHoursJson = dto.WorkingHoursJson
        };
        _personnelEs.Add(personnelEntity);
        await _uow.SaveChangesAsync();

        // Hizmet yetenekleri ekle
        var skillServiceIds = DistinctSkillServiceIds(dto.SkillServiceIds);
        if (skillServiceIds.Count > 0)
        {
            foreach (var serviceId in skillServiceIds)
                _skillEs.Add(new SlnPersonnelSkill { PersonnelId = personnelEntity.Id, ServiceId = serviceId });
            await _uow.SaveChangesAsync();
        }

        return (true, new PortalPersonnelListDto
        {
            Id = personnelEntity.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Title = personnelEntity.Title,
            CustomerRoleId = personnelEntity.CustomerRoleId,
            CustomerRoleName = SalonRoles.GetById(personnelEntity.CustomerRoleId)?.Description
                ?? CustomerRoles.GetById(personnelEntity.CustomerRoleId)?.Description,
            BranchId = personnelEntity.BranchId,
            SkillServiceIds = skillServiceIds,
            IsActive = true
        });
    }

    public async Task<(bool Success, string? Error)> UpdatePersonnelAsync(int customerId, int id, PortalPersonnelUpdateDto dto, bool isSystemAdmin = false)
    {
        var personnel = await _personnelEs.GetByIdWithUserAsync(id, customerId);
        if (personnel == null)
            return (false, "Personel bulunamadi.");

        // Salon Sahibi rolu degistirilemez
        if (dto.CustomerRoleId == SalonRoles.Ids.SalonOwner && personnel.CustomerRoleId != SalonRoles.Ids.SalonOwner)
            return (false, "Salon Sahibi rolu atanamaz.");
        if (personnel.CustomerRoleId == SalonRoles.Ids.SalonOwner && dto.CustomerRoleId != SalonRoles.Ids.SalonOwner)
            return (false, "Salon Sahibi rolunden baska bir role degistirilemez.");

        // Kullanici adi degisikligi sadece sistem admin tarafindan yapilabilir
        if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != personnel.User.UserName)
        {
            if (!isSystemAdmin)
                return (false, "Kullanici adi sadece sistem yoneticisi tarafindan degistirilebilir.");

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

        if (!dto.IsActive && personnel.IsActive
            && (personnel.IsCustomerAdmin || personnel.CustomerRoleId == CustomerRoles.Ids.FirmaAdmin))
        {
            var activeAdminCount = await _personnelEs.GetActiveAdminCountAsync(customerId);
            if (activeAdminCount <= 1)
                return (false, "Firmada en az bir yonetici olmalidir. Son yonetici deaktive edilemez.");
        }

        var dependencyError = await ValidatePersonnelDependenciesAsync(
            customerId,
            dto.BranchId,
            dto.ReportsToPersonnelId,
            dto.SkillServiceIds,
            currentPersonnelId: id);
        if (dependencyError != null)
            return (false, dependencyError);

        personnel.User.FullName = dto.FullName;
        personnel.User.Email = dto.Email;
        personnel.User.IsActive = dto.IsActive;
        personnel.IsActive = dto.IsActive;

        if (dto.Title != null)
            personnel.Title = dto.Title.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var passwordError = await ApplyPersonnelPasswordAsync(personnel, dto.Password);
            if (passwordError != null)
                return (false, passwordError);
        }

        if (dto.CustomerRoleId != personnel.CustomerRoleId)
        {
            personnel.CustomerRoleId = dto.CustomerRoleId;
            personnel.IsCustomerAdmin = dto.CustomerRoleId == CustomerRoles.Ids.FirmaAdmin;
        }

        personnel.OrganizationUnitId = dto.OrganizationUnitId;
        personnel.ReportsToPersonnelId = dto.ReportsToPersonnelId;
        personnel.BranchId = dto.BranchId;

        if (dto.PhotoUrl != null)
            personnel.PhotoUrl = dto.PhotoUrl;

        // Public gorunurluk
        personnel.PublicVisible = dto.PublicVisible;
        personnel.PublicShowFullName = dto.PublicShowFullName;
        personnel.PublicShowPhoto = dto.PublicShowPhoto;
        personnel.PublicShowTitle = dto.PublicShowTitle;
        personnel.PublicShowSpecialty = dto.PublicShowSpecialty;

        // Personel calisma saatleri (null/bos = sube saatleri kullanilir)
        personnel.WorkingHoursJson = dto.WorkingHoursJson;

        // Hizmet yetenekleri guncelle
        if (dto.SkillServiceIds != null)
        {
            var existingSkills = await _skillEs.GetAllQueryable()
                .Where(s => s.PersonnelId == id)
                .ToListAsync();
            foreach (var s in existingSkills) _skillEs.Remove(s);

            foreach (var serviceId in DistinctSkillServiceIds(dto.SkillServiceIds))
                _skillEs.Add(new SlnPersonnelSkill { PersonnelId = id, ServiceId = serviceId });
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPersonnelPasswordAsync(int customerId, int id, string password)
    {
        var personnel = await _personnelEs.GetByIdWithUserAsync(id, customerId);
        if (personnel == null) return (false, "Personel bulunamadı.");

        var passwordError = await ApplyPersonnelPasswordAsync(personnel, password);
        return passwordError == null ? (true, null) : (false, passwordError);
    }

    private async Task<string?> ApplyPersonnelPasswordAsync(CustomerPersonnel personnel, string password)
    {
        var (pwValid, pwErrors) = _passwordPolicy.ValidatePassword(password);
        if (!pwValid)
            return string.Join(" ", pwErrors);

        if (await _passwordPolicy.IsPasswordReusedAsync(personnel.UserId, password))
            return "Bu şifre daha önce kullanılmış. Farklı bir şifre seçiniz.";

        var newHash = BCrypt.Net.BCrypt.HashPassword(password);
        personnel.User.PasswordHash = newHash;
        personnel.User.PasswordChangedAt = DateTime.UtcNow;
        personnel.User.MustChangePassword = false;
        personnel.User.FailedLoginCount = 0;
        personnel.User.LockedUntil = null;

        await _uow.SaveChangesAsync();
        await _passwordPolicy.RecordPasswordAsync(personnel.UserId, newHash);
        return null;
    }

    private async Task<string?> ValidatePersonnelDependenciesAsync(
        int customerId,
        int? branchId,
        int? reportsToPersonnelId,
        List<int>? skillServiceIds,
        int? currentPersonnelId = null)
    {
        if (branchId.HasValue)
        {
            var branchExists = await _branchEs.GetAllQueryable()
                .AnyAsync(b => b.Id == branchId.Value && b.CustomerId == customerId && b.IsActive);
            if (!branchExists) return "Şube bulunamadı.";
        }

        if (reportsToPersonnelId.HasValue)
        {
            if (currentPersonnelId.HasValue && reportsToPersonnelId.Value == currentPersonnelId.Value)
                return "Personel kendi amiri olamaz.";

            var managerExists = await _personnelEs.GetAllQueryable()
                .AnyAsync(p => p.Id == reportsToPersonnelId.Value && p.CustomerId == customerId && p.IsActive);
            if (!managerExists) return "Amir personel bulunamadı.";
        }

        if (skillServiceIds is { Count: > 0 })
        {
            var distinctServiceIds = DistinctSkillServiceIds(skillServiceIds);
            if (distinctServiceIds.Count != skillServiceIds.Count)
                return "Hizmet yetenekleri tekrarsız ve geçerli olmalı.";

            var validServiceCount = await _serviceEs.GetAllQueryable()
                .CountAsync(s => s.CustomerId == customerId && distinctServiceIds.Contains(s.Id));
            if (validServiceCount != distinctServiceIds.Count)
                return "Hizmet yetenekleri bu salona ait olmalı.";
        }

        return null;
    }

    private static List<int> DistinctSkillServiceIds(List<int>? skillServiceIds)
        => skillServiceIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList() ?? [];

    public async Task<(bool Success, string? Error, string? PhotoUrl)> GetPersonnelPhotoUrlAsync(int customerId, int id)
    {
        var personnel = await _personnelEs.GetByIdWithUserAsync(id, customerId);
        if (personnel == null) return (false, "Personel bulunamadı.", null);

        return (true, null, personnel.PhotoUrl);
    }

    public async Task<(bool Success, string? Error)> UpdatePersonnelPhotoAsync(int customerId, int id, string photoUrl)
    {
        var personnel = await _personnelEs.GetByIdWithUserAsync(id, customerId);
        if (personnel == null) return (false, "Personel bulunamadı.");
        personnel.PhotoUrl = photoUrl;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeactivatePersonnelAsync(int customerId, int id)
    {
        var personnel = await _personnelEs.GetByIdWithUserAsync(id, customerId);
        if (personnel == null)
            return (false, "Personel bulunamadi.");

        if (personnel.IsCustomerAdmin || personnel.CustomerRoleId == CustomerRoles.Ids.FirmaAdmin)
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

    public async Task<(bool Success, string? Error)> ReactivatePersonnelAsync(int customerId, int id)
    {
        var personnel = await _personnelEs.GetByIdWithUserAsync(id, customerId);
        if (personnel == null)
            return (false, "Personel bulunamadi.");

        if (personnel.IsActive && personnel.User.IsActive)
            return (false, "Personel zaten aktif.");

        // MaxUsers kontrolü
        var customer = await _customerEs.GetByIdAsync(customerId);
        if (customer != null && personnel.CustomerRoleId != CustomerRoles.Ids.FirmaAdmin)
        {
            var activeCount = await _personnelEs.GetActiveCountAsync(customerId, excludeAdmin: true);
            if (activeCount >= customer.MaxUsers)
                return (false, $"Maksimum kullanici limitine ({customer.MaxUsers}) ulasildi. Yeni personel aktiflestirilemiyor.");
        }

        personnel.IsActive = true;
        personnel.User.IsActive = true;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnlockPersonnelAsync(int customerId, int id)
    {
        var personnel = await _personnelEs.GetByIdWithUserAsync(id, customerId);
        if (personnel == null)
            return (false, "Personel bulunamadi.");

        if (!personnel.User.LockedUntil.HasValue || personnel.User.LockedUntil.Value <= DateTime.UtcNow)
            return (false, "Hesap zaten kilitli degil.");

        personnel.User.LockedUntil = null;
        personnel.User.FailedLoginCount = 0;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    // REPORTS-TO (AMIR ATAMASI)

    public async Task<(bool Success, string? Error)> SetReportsToAsync(int customerId, int personnelId, int? reportsToPersonnelId)
    {
        var personnel = await _personnelEs.GetByIdWithUserAsync(personnelId, customerId);
        if (personnel == null)
            return (false, "Personel bulunamadi.");

        if (reportsToPersonnelId == personnelId)
            return (false, "Bir personel kendi amiri olamaz.");

        if (reportsToPersonnelId.HasValue)
        {
            var target = await _personnelEs.GetByIdWithUserAsync(reportsToPersonnelId.Value, customerId);
            if (target == null)
                return (false, "Hedef amir bulunamadi.");

            // Cycle detection: BFS ile reportsToPersonnelId'den yukarı çık, personnelId'ye ulaşılıyorsa döngü var
            var allPersonnel = await _personnelEs.GetAllQueryable()
                .Where(p => p.CustomerId == customerId)
                .Select(p => new { p.Id, p.ReportsToPersonnelId })
                .ToListAsync();

            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(reportsToPersonnelId.Value);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var parent = allPersonnel.FirstOrDefault(p => p.Id == current);
                if (parent?.ReportsToPersonnelId == null) continue;

                if (parent.ReportsToPersonnelId == personnelId)
                    return (false, "Dongusel iliski tespit edildi. Bu atama bir amir dongusune yol acar.");

                if (visited.Add(parent.ReportsToPersonnelId.Value))
                    queue.Enqueue(parent.ReportsToPersonnelId.Value);
            }
        }

        personnel.ReportsToPersonnelId = reportsToPersonnelId;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // PERSONNEL OPS

    public async Task<PortalPersonnelOpsDto> GetPersonnelOpsAsync(int customerId, DateTime from, DateTime to, int? callerPersonnelId = null, int? callerRoleId = null, int? callerBranchId = null)
    {
        var personnelQuery = BuildPersonnelScopeQuery(customerId, callerRoleId, callerBranchId);
        var personnel = await personnelQuery
            .Include(p => p.User)
            .Select(p => new { p.Id, p.User.FullName })
            .ToListAsync();
        var personnelIds = personnel.Select(p => p.Id).ToList();
        var nameMap = personnel.ToDictionary(p => p.Id, p => p.FullName);
        var fromDate = from.Date;
        var toDate = to.Date;

        var shifts = await _shiftEs.GetAllQueryable()
            .Where(s => personnelIds.Contains(s.PersonnelId) && s.ShiftDate.Date >= fromDate && s.ShiftDate.Date <= toDate)
            .OrderBy(s => s.ShiftDate).ThenBy(s => s.StartTime)
            .ToListAsync();
        var leaves = await _leaveEs.GetAllQueryable()
            .Where(l => personnelIds.Contains(l.PersonnelId) && l.StartDate.Date <= toDate && l.EndDate.Date >= fromDate)
            .OrderBy(l => l.StartDate)
            .ToListAsync();
        var timesheets = await _timesheetEs.GetAllQueryable()
            .Where(t => personnelIds.Contains(t.PersonnelId) && t.WorkDate.Date >= fromDate && t.WorkDate.Date <= toDate)
            .OrderBy(t => t.WorkDate)
            .ToListAsync();
        var advances = await _advanceEs.GetAllQueryable()
            .Where(a => personnelIds.Contains(a.PersonnelId) && a.AdvanceDate.Date >= fromDate && a.AdvanceDate.Date <= toDate)
            .OrderByDescending(a => a.AdvanceDate)
            .ToListAsync();
        var payrolls = await _payrollEs.GetAllQueryable()
            .Where(p => personnelIds.Contains(p.PersonnelId))
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Take(100)
            .ToListAsync();

        return new PortalPersonnelOpsDto
        {
            Shifts = shifts.Select(s => new PortalPersonnelShiftDto
            {
                Id = s.Id,
                PersonnelId = s.PersonnelId,
                PersonnelName = nameMap.GetValueOrDefault(s.PersonnelId) ?? "-",
                ShiftDate = s.ShiftDate,
                StartTime = s.StartTime.ToString(@"hh\:mm"),
                EndTime = s.EndTime.ToString(@"hh\:mm"),
                BreakMinutes = s.BreakMinutes,
                Notes = s.Notes
            }).ToList(),
            Leaves = leaves.Select(l => new PortalPersonnelLeaveDto
            {
                Id = l.Id,
                PersonnelId = l.PersonnelId,
                PersonnelName = nameMap.GetValueOrDefault(l.PersonnelId) ?? "-",
                LeaveTypeId = l.LeaveTypeId,
                LeaveTypeName = SalonLeaveTypes.GetById(l.LeaveTypeId)?.Description ?? l.LeaveTypeId.ToString(),
                StatusId = l.StatusId,
                StatusName = SalonLeaveStatuses.GetById(l.StatusId)?.Description ?? l.StatusId.ToString(),
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Notes = l.Notes
            }).ToList(),
            Timesheets = timesheets.Select(t => new PortalPersonnelTimesheetDto
            {
                Id = t.Id,
                PersonnelId = t.PersonnelId,
                PersonnelName = nameMap.GetValueOrDefault(t.PersonnelId) ?? "-",
                WorkDate = t.WorkDate,
                ClockInAt = t.ClockInAt,
                ClockOutAt = t.ClockOutAt,
                BreakMinutes = t.BreakMinutes,
                WorkedHours = CalculateWorkedHours(t.ClockInAt, t.ClockOutAt, t.BreakMinutes),
                Notes = t.Notes
            }).ToList(),
            Advances = advances.Select(a => new PortalAdvanceDto
            {
                Id = a.Id,
                PersonnelId = a.PersonnelId,
                PersonnelName = nameMap.GetValueOrDefault(a.PersonnelId) ?? "-",
                Amount = a.Amount,
                AdvanceDate = a.AdvanceDate,
                Notes = a.Notes
            }).ToList(),
            Payrolls = payrolls.Select(p => new PortalPayrollDto
            {
                Id = p.Id,
                PersonnelId = p.PersonnelId,
                PersonnelName = nameMap.GetValueOrDefault(p.PersonnelId) ?? "-",
                Year = p.Year,
                Month = p.Month,
                BaseSalary = p.BaseSalary,
                ServiceCommission = p.ServiceCommission,
                ProductCommission = p.ProductCommission,
                TotalAdvance = p.TotalAdvance,
                Deductions = p.Deductions,
                NetPay = p.NetPay,
                Notes = p.Notes,
                IsFinalized = p.IsFinalized
            }).ToList(),
            LeaveTypes = SalonLeaveTypes.All.Select(ToTypeItemDto).ToList(),
            LeaveStatuses = SalonLeaveStatuses.All.Select(ToTypeItemDto).ToList()
        };
    }

    public async Task<(bool Success, string? Error)> UpsertPersonnelShiftAsync(int customerId, int? id, PortalPersonnelShiftUpsertDto dto, int? callerRoleId = null, int? callerBranchId = null)
    {
        var personnel = await GetScopedPersonnelAsync(customerId, dto.PersonnelId, callerRoleId, callerBranchId);
        if (personnel == null) return (false, "Personel bulunamadi.");
        if (!TimeSpan.TryParse(dto.StartTime, out var start) || !TimeSpan.TryParse(dto.EndTime, out var end) || end <= start)
            return (false, "Gecerli vardiya saati giriniz.");

        var date = dto.ShiftDate.Date;
        var shift = id.HasValue
            ? await _shiftEs.GetAllQueryable().FirstOrDefaultAsync(s => s.Id == id.Value && s.PersonnelId == dto.PersonnelId)
            : await _shiftEs.GetAllQueryable().FirstOrDefaultAsync(s => s.PersonnelId == dto.PersonnelId && s.ShiftDate.Date == date);
        if (shift == null)
        {
            shift = new SlnPersonnelShift { PersonnelId = dto.PersonnelId };
            _shiftEs.Add(shift);
        }

        shift.ShiftDate = date;
        shift.StartTime = start;
        shift.EndTime = end;
        shift.BreakMinutes = Math.Max(0, dto.BreakMinutes);
        shift.Notes = dto.Notes;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeletePersonnelShiftAsync(int customerId, int id, int? callerRoleId = null, int? callerBranchId = null)
    {
        var shift = await _shiftEs.GetAllQueryable().FirstOrDefaultAsync(s => s.Id == id);
        if (shift == null) return (false, "Vardiya bulunamadi.");
        var personnel = await GetScopedPersonnelAsync(customerId, shift.PersonnelId, callerRoleId, callerBranchId);
        if (personnel == null) return (false, "Personel bulunamadi.");
        _shiftEs.Remove(shift);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CreatePersonnelLeaveAsync(int customerId, PortalPersonnelLeaveCreateDto dto, int? callerRoleId = null, int? callerBranchId = null)
    {
        var personnel = await GetScopedPersonnelAsync(customerId, dto.PersonnelId, callerRoleId, callerBranchId);
        if (personnel == null) return (false, "Personel bulunamadi.");
        if (SalonLeaveTypes.GetById(dto.LeaveTypeId) == null) return (false, "Izin tipi gecersiz.");
        if (dto.EndDate.Date < dto.StartDate.Date) return (false, "Izin bitis tarihi baslangictan once olamaz.");

        _leaveEs.Add(new SlnPersonnelLeave
        {
            PersonnelId = dto.PersonnelId,
            LeaveTypeId = dto.LeaveTypeId,
            StatusId = SalonLeaveStatuses.Ids.Pending,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            Notes = dto.Notes
        });
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdatePersonnelLeaveStatusAsync(
        int customerId,
        int id,
        PortalPersonnelLeaveStatusDto dto,
        int? reviewedByPersonnelId,
        int? callerRoleId = null,
        int? callerBranchId = null)
    {
        if (SalonLeaveStatuses.GetById(dto.StatusId) == null) return (false, "Izin durumu gecersiz.");
        var leave = await _leaveEs.GetAllQueryable().FirstOrDefaultAsync(l => l.Id == id);
        if (leave == null) return (false, "Izin kaydi bulunamadi.");
        var personnel = await GetScopedPersonnelAsync(customerId, leave.PersonnelId, callerRoleId, callerBranchId);
        if (personnel == null) return (false, "Personel bulunamadi.");
        leave.StatusId = dto.StatusId;
        leave.ReviewedByPersonnelId = reviewedByPersonnelId;
        leave.ReviewedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpsertPersonnelTimesheetAsync(int customerId, int? id, PortalPersonnelTimesheetUpsertDto dto, int? callerRoleId = null, int? callerBranchId = null)
    {
        var personnel = await GetScopedPersonnelAsync(customerId, dto.PersonnelId, callerRoleId, callerBranchId);
        if (personnel == null) return (false, "Personel bulunamadi.");
        if (dto.ClockInAt.HasValue && dto.ClockOutAt.HasValue && dto.ClockOutAt <= dto.ClockInAt)
            return (false, "Cikis saati giristen once olamaz.");

        var date = dto.WorkDate.Date;
        var row = id.HasValue
            ? await _timesheetEs.GetAllQueryable().FirstOrDefaultAsync(t => t.Id == id.Value && t.PersonnelId == dto.PersonnelId)
            : await _timesheetEs.GetAllQueryable().FirstOrDefaultAsync(t => t.PersonnelId == dto.PersonnelId && t.WorkDate.Date == date);
        if (row == null)
        {
            row = new SlnPersonnelTimesheet { PersonnelId = dto.PersonnelId };
            _timesheetEs.Add(row);
        }

        row.WorkDate = date;
        row.ClockInAt = dto.ClockInAt;
        row.ClockOutAt = dto.ClockOutAt;
        row.BreakMinutes = Math.Max(0, dto.BreakMinutes);
        row.Notes = dto.Notes;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CreatePersonnelAdvanceAsync(int customerId, PortalAdvanceCreateDto dto, int? callerRoleId = null, int? callerBranchId = null)
    {
        var personnel = await GetScopedPersonnelAsync(customerId, dto.PersonnelId, callerRoleId, callerBranchId);
        if (personnel == null) return (false, "Personel bulunamadi.");
        if (dto.Amount <= 0) return (false, "Avans tutari sifirdan buyuk olmalidir.");
        _advanceEs.Add(new SlnAdvance
        {
            PersonnelId = dto.PersonnelId,
            Amount = dto.Amount,
            AdvanceDate = dto.AdvanceDate.Date,
            Notes = dto.Notes
        });
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> GeneratePayrollAsync(int customerId, PortalPayrollGenerateDto dto, int? callerRoleId = null, int? callerBranchId = null)
    {
        var personnel = await GetScopedPersonnelAsync(customerId, dto.PersonnelId, callerRoleId, callerBranchId);
        if (personnel == null) return (false, "Personel bulunamadi.");
        if (dto.Year < 2000 || dto.Month is < 1 or > 12) return (false, "Bordro donemi gecersiz.");
        var start = new DateTime(dto.Year, dto.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var items = await _invoiceItemEs.GetAllQueryable()
            .Include(i => i.Invoice)
            .Where(i => i.PersonnelId == dto.PersonnelId
                && i.Invoice != null
                && i.Invoice.CustomerId == customerId
                && i.Invoice.InvoiceDate >= start
                && i.Invoice.InvoiceDate < end)
            .ToListAsync();
        var commissions = await GetCommissionsForPayrollAsync(dto.PersonnelId);
        var serviceCommission = CalculateCommission(items.Where(i => i.ServiceId.HasValue), commissions);
        var productCommission = CalculateCommission(items.Where(i => i.ProductId.HasValue), commissions);
        var totalAdvance = await _advanceEs.GetAllQueryable()
            .Where(a => a.PersonnelId == dto.PersonnelId && a.AdvanceDate >= start && a.AdvanceDate < end)
            .SumAsync(a => a.Amount);

        var payroll = await _payrollEs.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.PersonnelId == dto.PersonnelId && p.Year == dto.Year && p.Month == dto.Month);
        if (payroll?.IsFinalized == true)
            return (false, "Kesinlesmis bordro guncellenemez.");
        if (payroll == null)
        {
            payroll = new SlnPayroll { PersonnelId = dto.PersonnelId, Year = dto.Year, Month = dto.Month };
            _payrollEs.Add(payroll);
        }

        payroll.BaseSalary = dto.BaseSalary;
        payroll.ServiceCommission = serviceCommission;
        payroll.ProductCommission = productCommission;
        payroll.TotalAdvance = totalAdvance;
        payroll.Deductions = dto.Deductions;
        payroll.NetPay = dto.BaseSalary + serviceCommission + productCommission - totalAdvance - dto.Deductions;
        payroll.Notes = dto.Notes;
        payroll.IsFinalized = dto.IsFinalized;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private IQueryable<CustomerPersonnel> BuildPersonnelScopeQuery(int customerId, int? callerRoleId, int? callerBranchId)
    {
        var query = _personnelEs.GetAllQueryable().Where(p => p.CustomerId == customerId);
        if (callerRoleId == SalonRoles.Ids.BranchManager && !callerBranchId.HasValue)
            return query.Where(_ => false);

        var shouldApplyBranchScope = callerBranchId.HasValue
            && callerRoleId != SalonRoles.Ids.SalonOwner
            && callerRoleId != CustomerRoles.Ids.FirmaAdmin;
        return shouldApplyBranchScope && callerBranchId is int branchId ? query.Where(p => p.BranchId == branchId) : query;
    }

    private Task<CustomerPersonnel?> GetScopedPersonnelAsync(int customerId, int personnelId, int? callerRoleId, int? callerBranchId)
        => BuildPersonnelScopeQuery(customerId, callerRoleId, callerBranchId).FirstOrDefaultAsync(p => p.Id == personnelId);

    private static decimal CalculateWorkedHours(DateTime? clockInAt, DateTime? clockOutAt, int breakMinutes)
    {
        if (!clockInAt.HasValue || !clockOutAt.HasValue || clockOutAt <= clockInAt) return 0;
        var minutes = (decimal)(clockOutAt.Value - clockInAt.Value).TotalMinutes - Math.Max(0, breakMinutes);
        return Math.Round(Math.Max(0, minutes) / 60, 2);
    }

    private async Task<List<SlnPersonnelCommission>> GetCommissionsForPayrollAsync(int personnelId)
        => await _commissionEs.GetAllQueryable().Where(c => c.PersonnelId == personnelId).ToListAsync();

    private static decimal CalculateCommission(IEnumerable<SlnInvoiceItem> items, List<SlnPersonnelCommission> commissions)
    {
        decimal total = 0;
        foreach (var item in items)
        {
            var rule = commissions.FirstOrDefault(c => c.ServiceId == item.ServiceId && c.ProductId == item.ProductId)
                ?? commissions.FirstOrDefault(c => c.ServiceId == item.ServiceId && c.ProductId == null)
                ?? commissions.FirstOrDefault(c => c.ProductId == item.ProductId && c.ServiceId == null)
                ?? commissions.FirstOrDefault(c => c.ServiceId == null && c.ProductId == null);
            if (rule == null) continue;
            total += rule.IsPercentage ? item.LineTotal * rule.Rate / 100 : rule.Rate * item.Quantity;
        }
        return total;
    }

    private static TypeItemDto ToTypeItemDto(TypeItem item) => new()
    {
        Id = item.Id,
        SystemName = item.SystemName,
        Description = item.Description ?? item.SystemName,
        Icon = item.Icon,
        ColorClass = item.CssClass
    };

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
            .Include(s => s.Lines)
            .Where(s => s.CustomerId == customerId)
            .Select(s => new PortalSipAccountDto
            {
                Id = s.Id,
                Name = s.Name,
                Server = s.Server,
                Port = s.Port,
                Transport = s.Transport,
                IsDefault = s.IsDefault,
                IsActive = s.IsActive,
                LineCount = s.Lines.Count,
                ActiveLineCount = s.Lines.Count(l => l.IsActive),
                Lines = s.Lines.Select(l => new PortalSipLineDto
                {
                    Id = l.Id,
                    ChannelNumber = l.ChannelNumber,
                    Username = l.Username,
                    Description = l.Description,
                    IsActive = l.IsActive
                }).OrderBy(l => l.ChannelNumber).ToList()
            })
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateSipAccountAsync(int customerId, int id, PortalSipUpdateDto dto)
    {
        var account = await _sipEs.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customerId);
        if (account == null)
            return (false, "Gateway bulunamadi.");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            account.Name = dto.Name;

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
            return (false, null, "Bu isimde bir gateway zaten mevcut.");

        var account = new SipAccount
        {
            CustomerId = customerId,
            Name = dto.Name,
            Server = dto.Server,
            Port = dto.Port,
            Transport = dto.Transport,
            IsDefault = dto.IsDefault,
            IsActive = true
        };

        // Gateway ile birlikte hatlar da eklenebilir
        if (dto.Lines?.Count > 0)
        {
            foreach (var lineDto in dto.Lines)
            {
                account.Lines.Add(new SipLine
                {
                    ChannelNumber = lineDto.ChannelNumber,
                    Username = lineDto.Username,
                    Password = _encryption.Encrypt(lineDto.Password),
                    Description = lineDto.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

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

    public async Task<(bool Success, string? Error)> DeleteSipAccountAsync(int customerId, int id)
    {
        var account = await _sipEs.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customerId);
        if (account == null)
            return (false, "Gateway bulunamadi.");

        account.IsActive = false;
        await _uow.SaveChangesAsync();

        return (true, null);
    }
}

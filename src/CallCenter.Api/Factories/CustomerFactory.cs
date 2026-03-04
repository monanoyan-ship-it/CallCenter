using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class CustomerFactory : ICustomerFactory
{
    private readonly ICustomerEntityService _customerEs;
    private readonly ICustomerPersonnelEntityService _personnelEs;
    private readonly ICustomerPortalModuleEntityService _moduleEs;
    private readonly IUserEntityService _userEs;
    private readonly IPasswordPolicyFactory _passwordPolicy;
    private readonly IUnitOfWork _uow;

    public CustomerFactory(
        ICustomerEntityService customerEs,
        ICustomerPersonnelEntityService personnelEs,
        ICustomerPortalModuleEntityService moduleEs,
        IUserEntityService userEs,
        IPasswordPolicyFactory passwordPolicy,
        IUnitOfWork uow)
    {
        _customerEs = customerEs;
        _personnelEs = personnelEs;
        _moduleEs = moduleEs;
        _userEs = userEs;
        _passwordPolicy = passwordPolicy;
        _uow = uow;
    }

    // CUSTOMERS

    public async Task<PagedResult<CustomerListDto>> GetAllAsync(int page, int pageSize, string? search)
    {
        var query = _customerEs.GetAllQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(s)
                                  || (c.Email != null && c.Email.ToLower().Contains(s))
                                  || (c.Phone != null && c.Phone.Contains(s)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Email = c.Email,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                MaxUsers = c.MaxUsers,
                MonthlyUnitPrice = c.MonthlyUnitPrice,
                PersonnelCount = c.Personnel.Count,
                QueueCount = c.Queues.Count,
                SipAccountCount = c.SipAccounts.Count
            })
            .ToListAsync();

        return new PagedResult<CustomerListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDetailDto?> GetByIdAsync(int id)
    {
        var c = await _customerEs.GetByIdWithPersonnelAsync(id);
        if (c == null) return null;

        var adminPersonnel = c.Personnel.FirstOrDefault(p => p.IsCustomerAdmin);
        CustomerAdminInfoDto? adminInfo = null;
        if (adminPersonnel != null)
        {
            adminInfo = new CustomerAdminInfoDto
            {
                PersonnelId = adminPersonnel.Id,
                UserId = adminPersonnel.User.Id,
                UserName = adminPersonnel.User.UserName,
                FullName = adminPersonnel.User.FullName,
                Email = adminPersonnel.User.Email,
                Title = adminPersonnel.Title,
                LastLoginAt = adminPersonnel.User.LastLoginAt,
                IsActive = adminPersonnel.User.IsActive
            };
        }

        return new CustomerDetailDto
        {
            Id = c.Id,
            Name = c.Name,
            TaxNumber = c.TaxNumber,
            Address = c.Address,
            Phone = c.Phone,
            Email = c.Email,
            IsActive = c.IsActive,
            MaxUsers = c.MaxUsers,
            MonthlyUnitPrice = c.MonthlyUnitPrice,
            CreatedAt = c.CreatedAt,
            Personnel = c.Personnel.Select(p => new PersonnelSimpleDto
            {
                Id = p.Id,
                FullName = p.User.FullName,
                Title = p.Title
            }).ToList(),
            AdminInfo = adminInfo
        };
    }

    public async Task<(int Id, string? Error)> CreateAsync(CustomerCreateDto dto)
    {
        var userName = dto.AdminUserName.Trim();
        if (await _userEs.ExistsByUsernameAsync(userName))
            return (0, "Bu kullanici adi zaten kullaniliyor.");

        var customer = new Customer
        {
            Name = dto.Name,
            TaxNumber = dto.TaxNumber,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            MaxUsers = dto.MaxUsers,
            MonthlyUnitPrice = dto.MonthlyUnitPrice,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _customerEs.Add(customer);
        await _uow.SaveChangesAsync();

        foreach (var module in PortalModules.All)
        {
            _moduleEs.Add(new CustomerPortalModule
            {
                CustomerId = customer.Id,
                ModuleId = module.Id,
                IsActive = true,
                ActivatedAt = DateTime.UtcNow
            });
        }
        await _uow.SaveChangesAsync();

        await CreateCustomerAdminAsync(customer, userName, dto.AdminPassword);

        return (customer.Id, null);
    }

    private async Task CreateCustomerAdminAsync(Customer customer, string userName, string password)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var adminUser = new User
        {
            UserName = userName,
            FullName = $"{customer.Name} Yönetici",
            Email = customer.Email ?? $"{userName}@placeholder.local",
            PasswordHash = passwordHash,
            RoleId = UserRoles.Ids.CustomerUser,
            StatusId = AgentStatuses.Ids.Offline,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PasswordChangedAt = DateTime.UtcNow,
            MustChangePassword = true
        };
        _userEs.Add(adminUser);
        await _uow.SaveChangesAsync();

        await _passwordPolicy.RecordPasswordAsync(adminUser.Id, passwordHash);

        var adminPersonnel = new CustomerPersonnel
        {
            UserId = adminUser.Id,
            CustomerId = customer.Id,
            Title = "Müşteri Yöneticisi",
            CustomerRoleId = CustomerRoles.Ids.FirmaAdmin,
            IsCustomerAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _personnelEs.Add(adminPersonnel);
        await _uow.SaveChangesAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, CustomerUpdateDto dto)
    {
        var customer = await _customerEs.GetByIdAsync(id);
        if (customer == null) return (false, "Musteri bulunamadi.");

        customer.Name = dto.Name;
        customer.TaxNumber = dto.TaxNumber;
        customer.Address = dto.Address;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.IsActive = dto.IsActive;
        customer.MaxUsers = dto.MaxUsers;
        customer.MonthlyUnitPrice = dto.MonthlyUnitPrice;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var customer = await _customerEs.GetByIdAsync(id);
        if (customer == null) return (false, "Musteri bulunamadi.");

        customer.IsActive = false;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? TempPassword, string? Error)> ResetAdminPasswordAsync(int customerId)
    {
        var admin = await _personnelEs.GetCustomerAdminAsync(customerId);
        if (admin == null)
            return (false, null, "Bu musterinin admin kullanicisi bulunamadi.");

        var tempPassword = _passwordPolicy.GenerateSecureTemporaryPassword();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
        admin.User.PasswordHash = passwordHash;
        admin.User.PasswordChangedAt = DateTime.UtcNow;
        admin.User.MustChangePassword = true;
        await _uow.SaveChangesAsync();

        await _passwordPolicy.RecordPasswordAsync(admin.User.Id, passwordHash);

        return (true, tempPassword, null);
    }

    // MODULES

    public async Task<object?> GetCustomerModulesAsync(int customerId)
    {
        var customer = await _customerEs.GetByIdWithPortalModulesAsync(customerId);
        if (customer == null) return null;

        return PortalModules.All.Select(m =>
        {
            var assigned = customer.PortalModules.FirstOrDefault(pm => pm.ModuleId == m.Id);
            return new PortalModuleDto
            {
                Id = m.Id,
                SystemName = m.SystemName,
                Description = m.Description,
                Icon = m.Icon,
                IsActive = assigned?.IsActive ?? false,
                Permissions = CustomerPermissionTypes.GetByModule(m.Id).Select(p => new PermissionTypeDto
                {
                    Id = p.Id,
                    SystemName = p.SystemName,
                    Description = p.Description,
                    Icon = p.Icon,
                    ModuleId = m.Id,
                    ModuleName = m.SystemName
                }).ToList()
            };
        }).ToList();
    }

    public async Task<(bool Success, string? Error)> AssignModulesAsync(int customerId, AssignModulesRequest request)
    {
        var customer = await _customerEs.GetByIdWithPortalModulesAsync(customerId);
        if (customer == null) return (false, "Müşteri bulunamadı.");

        foreach (var moduleId in request.ModuleIds)
        {
            if (PortalModules.GetById(moduleId) == null) continue;

            var existing = customer.PortalModules.FirstOrDefault(m => m.ModuleId == moduleId);
            if (existing != null)
            {
                existing.IsActive = true;
                existing.DeactivatedAt = null;
            }
            else
            {
                _moduleEs.Add(new CustomerPortalModule
                {
                    CustomerId = customerId,
                    ModuleId = moduleId,
                    IsActive = true,
                    Notes = request.Notes
                });
            }
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeactivateModuleAsync(int customerId, int moduleId)
    {
        var module = await _moduleEs.GetByCustomerAndModuleAsync(customerId, moduleId);
        if (module == null) return (false, "Modül ataması bulunamadı.");

        module.IsActive = false;
        module.DeactivatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

}

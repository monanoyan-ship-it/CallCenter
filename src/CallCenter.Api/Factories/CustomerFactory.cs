using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Helpers;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CallCenter.Api.Factories;

public class CustomerFactory : ICustomerFactory
{
    private readonly ICustomerEntityService _customerEs;
    private readonly ICustomerPersonnelEntityService _personnelEs;
    private readonly ICustomerPortalModuleEntityService _moduleEs;
    private readonly IUserEntityService _userEs;
    private readonly ICustomerProductEntityService _customerProductEs;
    private readonly IModulePricingEntityService _modulePricingEs;
    private readonly ISlnServiceCategoryEntityService _serviceCategoryEs;
    private readonly ISlnServiceEntityService _serviceEs;
    private readonly ISlnExpenseCategoryEntityService _expenseCategoryEs;
    private readonly ISlnCashRegisterEntityService _cashRegisterEs;
    private readonly ISlnBranchEntityService _branchEs;
    private readonly ISubscriptionPlanEntityService _planEs;
    private readonly ICustomerSubscriptionEntityService _subscriptionEs;
    private readonly IPasswordPolicyFactory _passwordPolicy;
    private readonly IAuthFactory _authFactory;
    private readonly ISubscriptionFactory _subscriptionFactory;
    private readonly IUnitOfWork _uow;

    public CustomerFactory(
        ICustomerEntityService customerEs,
        ICustomerPersonnelEntityService personnelEs,
        ICustomerPortalModuleEntityService moduleEs,
        IUserEntityService userEs,
        ICustomerProductEntityService customerProductEs,
        IModulePricingEntityService modulePricingEs,
        ISlnServiceCategoryEntityService serviceCategoryEs,
        ISlnServiceEntityService serviceEs,
        ISlnExpenseCategoryEntityService expenseCategoryEs,
        ISlnCashRegisterEntityService cashRegisterEs,
        ISlnBranchEntityService branchEs,
        ISubscriptionPlanEntityService planEs,
        ICustomerSubscriptionEntityService subscriptionEs,
        IPasswordPolicyFactory passwordPolicy,
        IAuthFactory authFactory,
        ISubscriptionFactory subscriptionFactory,
        IUnitOfWork uow)
    {
        _customerEs = customerEs;
        _personnelEs = personnelEs;
        _moduleEs = moduleEs;
        _userEs = userEs;
        _customerProductEs = customerProductEs;
        _modulePricingEs = modulePricingEs;
        _serviceCategoryEs = serviceCategoryEs;
        _serviceEs = serviceEs;
        _expenseCategoryEs = expenseCategoryEs;
        _cashRegisterEs = cashRegisterEs;
        _branchEs = branchEs;
        _planEs = planEs;
        _subscriptionEs = subscriptionEs;
        _passwordPolicy = passwordPolicy;
        _authFactory = authFactory;
        _subscriptionFactory = subscriptionFactory;
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
                Products = c.Products.Where(p => p.IsActive).Select(p => new CustomerProductDto
                {
                    Id = p.Id,
                    ProductTypeId = p.ProductTypeId,
                    ProductTypeName = "",
                    MonthlyPrice = p.MonthlyPrice,
                    IsActive = p.IsActive
                }).ToList(),
                PersonnelCount = c.Personnel.Count,
                QueueCount = c.Queues.Count,
                SipAccountCount = c.SipAccounts.Count,
                IsTest = c.IsTest
            })
            .ToListAsync();

        // EF projection icinde static method cagrilamaz, post-process ile doldur
        foreach (var item in items)
            foreach (var p in item.Products)
                p.ProductTypeName = ProductTypes.GetById(p.ProductTypeId)?.Description ?? "?";

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

        // Products'i ayri yukle (GetByIdWithPersonnelAsync Products'i Include etmeyebilir)
        var products = await _customerProductEs.GetAllQueryable()
            .Where(cp => cp.CustomerId == c.Id && cp.IsActive)
            .ToListAsync();

        var dto = new CustomerDetailDto
        {
            Id = c.Id,
            Name = c.Name,
            TaxNumber = c.TaxNumber,
            Address = c.Address,
            Phone = c.Phone,
            Email = c.Email,
            IsActive = c.IsActive,
            MaxUsers = c.MaxUsers,
            Products = products.Select(p => new CustomerProductDto
            {
                Id = p.Id,
                ProductTypeId = p.ProductTypeId,
                ProductTypeName = ProductTypes.GetById(p.ProductTypeId)?.Description ?? "?",
                MonthlyPrice = p.MonthlyPrice,
                IsActive = p.IsActive
            }).ToList(),
            SaveRecordingToPlatform = c.SaveRecordingToPlatform,
            SaveRecordingToOwnStorage = c.SaveRecordingToOwnStorage,
            AutoRecordCalls = c.AutoRecordCalls,
            IsCallbackManagementEnabled = c.IsCallbackManagementEnabled,
            CreatedAt = c.CreatedAt,
            BillingAnchorDay = c.BillingAnchorDay,
            TimeZone = c.TimeZone,
            IsTest = c.IsTest,
            TestNotes = c.TestNotes,
            Personnel = c.Personnel.Select(p => new PersonnelSimpleDto
            {
                Id = p.Id,
                FullName = p.User.FullName,
                Title = p.Title
            }).ToList(),
            AdminInfo = adminInfo
        };

        if (products.Any(p => p.ProductTypeId == ProductTypes.Ids.Salon))
        {
            var activeSub = await _subscriptionEs.GetAllQueryable()
                .FirstOrDefaultAsync(s => s.CustomerId == c.Id && s.StatusId == SubscriptionStatuses.Ids.Active);
            if (activeSub != null)
            {
                dto.SalonSubscriptionDisplayMonthly = activeSub.MonthlyPrice;
                dto.SalonSubscriptionIsTrialOrUncontracted = activeSub.PeriodPrice == 0m;
            }
        }

        return dto;
    }

    public async Task<Customer?> GetRawByIdAsync(int id)
    {
        return await _customerEs.GetByIdAsync(id);
    }

    public async Task<(bool Success, string? Error)> UpdateSettingsAsync(int id, UpdateCustomerSettingsRequest dto)
    {
        var customer = await _customerEs.GetByIdAsync(id);
        if (customer == null) return (false, "Musteri bulunamadi.");

        if (dto.AutoRecordCalls.HasValue)
            customer.AutoRecordCalls = dto.AutoRecordCalls.Value;
        
        if (dto.IsCallbackManagementEnabled.HasValue)
            customer.IsCallbackManagementEnabled = dto.IsCallbackManagementEnabled.Value;

        await _uow.SaveChangesAsync();
        return (true, null);
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
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _customerEs.Add(customer);
        await _uow.SaveChangesAsync();

        // Musteri urunlerini ekle
        var products = dto.Products.Count > 0
            ? dto.Products
            : new List<CustomerProductInputDto> { new() { ProductTypeId = ProductTypes.Ids.CallCenter, MonthlyPrice = 0 } };

        foreach (var p in products)
        {
            _customerProductEs.Add(new CustomerProduct
            {
                CustomerId = customer.Id,
                ProductTypeId = p.ProductTypeId,
                MonthlyPrice = p.MonthlyPrice,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _uow.SaveChangesAsync();

        foreach (var module in PortalModules.All)
        {
            _moduleEs.Add(new CustomerPortalModule
            {
                CustomerId = customer.Id,
                ModuleId = module.Id,
                IsActive = module.IsDefault,
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
        if (dto.SaveRecordingToPlatform.HasValue)
            customer.SaveRecordingToPlatform = dto.SaveRecordingToPlatform.Value;
        if (dto.SaveRecordingToOwnStorage.HasValue)
            customer.SaveRecordingToOwnStorage = dto.SaveRecordingToOwnStorage.Value;
        if (dto.AutoRecordCalls.HasValue)
            customer.AutoRecordCalls = dto.AutoRecordCalls.Value;
        if (dto.IsCallbackManagementEnabled.HasValue)
            customer.IsCallbackManagementEnabled = dto.IsCallbackManagementEnabled.Value;
        if (!string.IsNullOrEmpty(dto.TimeZone)) customer.TimeZone = dto.TimeZone;
        customer.IsTest = dto.IsTest;
        customer.TestNotes = dto.TestNotes;

        // Products sync
        var existingProducts = await _customerProductEs.GetAllQueryable()
            .Where(cp => cp.CustomerId == id)
            .ToListAsync();

        var dtoProductTypeIds = dto.Products.Select(p => p.ProductTypeId).ToHashSet();

        foreach (var p in dto.Products)
        {
            var existing = existingProducts.FirstOrDefault(ep => ep.ProductTypeId == p.ProductTypeId);
            if (existing != null)
            {
                existing.MonthlyPrice = p.MonthlyPrice;
                existing.IsActive = true;
            }
            else
            {
                _customerProductEs.Add(new CustomerProduct
                {
                    CustomerId = id,
                    ProductTypeId = p.ProductTypeId,
                    MonthlyPrice = p.MonthlyPrice,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Listede olmayan urunleri deaktif et
        foreach (var ep in existingProducts.Where(ep => !dtoProductTypeIds.Contains(ep.ProductTypeId)))
            ep.IsActive = false;

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

    public async Task<(bool Success, string? Error)> HardDeleteAsync(int id)
    {
        var customer = await _customerEs.GetByIdWithPersonnelAsync(id);
        if (customer == null) return (false, "Musteri bulunamadi.");

        // Personel'e bagli User'lari sil
        foreach (var p in customer.Personnel.ToList())
        {
            if (p.User != null)
                _userEs.Remove(p.User);
        }

        // Customer silinince cascade ile silinir: PortalModules, OrganizationUnits, Queues, SipAccounts
        // Personnel cascade yok, EF tracked oldugu icin otomatik silinir
        _customerEs.Remove(customer);
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

        var pricings = await _modulePricingEs.GetAllAsync();
        var pricingMap = pricings.ToDictionary(p => p.ModuleId);

        return customer.PortalModules
            .Select(pm =>
            {
                var ccDef = PortalModules.GetById(pm.ModuleId);
                var slnDef = SalonPortalModules.GetById(pm.ModuleId);
                var moduleDef = ccDef ?? slnDef;
                if (moduleDef == null) return null;
                pricingMap.TryGetValue(pm.ModuleId, out var pricing);
                return new PortalModuleDto
                {
                    Id = moduleDef.Id,
                    SystemName = moduleDef.SystemName,
                    Description = moduleDef.Description,
                    Icon = moduleDef.Icon,
                    IsActive = pm.IsActive,
                    IsDefault = moduleDef.IsDefault,
                    ProductTypeId = ccDef != null ? PortalModules.ProductTypeId : SalonPortalModules.ProductTypeId,
                    GroupId = SalonModuleGroups.GetGroupId(pm.ModuleId),
                    GroupName = SalonModuleGroups.GetById(SalonModuleGroups.GetGroupId(pm.ModuleId) ?? 0)?.Description,
                    CatalogPrice = pricing?.MonthlyPrice ?? 0,
                    CustomerPrice = pm.MonthlyPrice,
                    TrialEndsAt = pm.TrialEndsAt,
                    IsImplemented = SalonPortalModules.IsImplemented(pm.ModuleId),
                    Permissions = CustomerPermissionTypes.GetByModule(moduleDef.Id).Select(p => new PermissionTypeDto
                    {
                        Id = p.Id,
                        SystemName = p.SystemName,
                        Description = p.Description,
                        Icon = p.Icon,
                        ModuleId = moduleDef.Id,
                        ModuleName = moduleDef.SystemName
                    }).ToList()
                };
            })
            .Where(dto => dto != null)
            .OrderBy(dto => dto!.Id)
            .ToList();
    }

    public async Task<(bool Success, string? Error)> AssignModulesAsync(int customerId, AssignModulesRequest request)
    {
        var customer = await _customerEs.GetByIdWithPortalModulesAsync(customerId);
        if (customer == null) return (false, "Müşteri bulunamadı.");

        foreach (var moduleId in request.ModuleIds)
        {
            if (PortalModules.GetById(moduleId) == null && SalonPortalModules.GetById(moduleId) == null) continue;

            var existing = customer.PortalModules.FirstOrDefault(m => m.ModuleId == moduleId);
            if (existing != null)
            {
                existing.IsActive = true;
                existing.DeactivatedAt = null;
                if (request.MonthlyPrice.HasValue) existing.MonthlyPrice = request.MonthlyPrice;
                if (request.TrialEndsAt.HasValue) existing.TrialEndsAt = request.TrialEndsAt;
            }
            else
            {
                _moduleEs.Add(new CustomerPortalModule
                {
                    CustomerId = customerId,
                    ModuleId = moduleId,
                    IsActive = true,
                    Notes = request.Notes,
                    MonthlyPrice = request.MonthlyPrice,
                    TrialEndsAt = request.TrialEndsAt
                });
            }
        }

        await _uow.SaveChangesAsync();
        await TryRefreshSalonDisplayMonthlyAsync(customerId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeactivateModuleAsync(int customerId, int moduleId)
    {
        var module = await _moduleEs.GetByCustomerAndModuleAsync(customerId, moduleId);
        if (module == null) return (false, "Modül ataması bulunamadı.");

        module.IsActive = false;
        module.DeactivatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        await TryRefreshSalonDisplayMonthlyAsync(customerId);

        return (true, null);
    }

    public async Task<(bool Success, int AddedCount, string? Error)> SyncMissingModulesAsync(int customerId)
    {
        var customer = await _customerEs.GetByIdWithPortalModulesAsync(customerId);
        if (customer == null) return (false, 0, "Müşteri bulunamadı.");

        // Hangi urunleri kullaniyor? Ona gore modulleri senkronize et
        var products = await _customerProductEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && p.IsActive)
            .Select(p => p.ProductTypeId)
            .ToListAsync();

        var allModules = new List<TypeItem>();
        if (products.Contains(PortalModules.ProductTypeId))
            allModules.AddRange(PortalModules.All);
        if (products.Contains(SalonPortalModules.ProductTypeId))
            allModules.AddRange(SalonPortalModules.All);
        // Hicbir urun yoksa default olarak Salon ekle
        if (allModules.Count == 0)
            allModules.AddRange(SalonPortalModules.All);

        var existingModuleIds = customer.PortalModules.Select(pm => pm.ModuleId).ToHashSet();
        var addedCount = 0;

        foreach (var module in allModules)
        {
            if (existingModuleIds.Contains(module.Id)) continue;

            _moduleEs.Add(new CustomerPortalModule
            {
                CustomerId = customerId,
                ModuleId = module.Id,
                IsActive = module.IsDefault,
                Notes = "Otomatik senkronizasyon ile eklendi"
            });
            addedCount++;
        }

        if (addedCount > 0)
        {
            await _uow.SaveChangesAsync();
            await TryRefreshSalonDisplayMonthlyAsync(customerId);
        }

        return (true, addedCount, null);
    }

    private async Task TryRefreshSalonDisplayMonthlyAsync(int customerId)
    {
        try
        {
            await _subscriptionFactory.RefreshSubscriptionDisplayMonthlyPriceAsync(customerId);
        }
        catch
        {
            /* Modul ataması başarılı kalsın; özet alanı gecikmeli güncellenebilir */
        }
    }

    public async Task<(bool Success, int RemovedCount, string? Error)> CleanupModulesAsync(int customerId, string keep)
    {
        var customer = await _customerEs.GetByIdWithPortalModulesAsync(customerId);
        if (customer == null) return (false, 0, "Müşteri bulunamadı.");

        var salonIds = SalonPortalModules.All.Select(m => m.Id).ToHashSet();
        var ccIds = PortalModules.All.Select(m => m.Id).ToHashSet();

        var toRemove = keep.ToLower() switch
        {
            "salon" => customer.PortalModules.Where(m => ccIds.Contains(m.ModuleId)).ToList(),
            "callcenter" => customer.PortalModules.Where(m => salonIds.Contains(m.ModuleId)).ToList(),
            _ => new List<CustomerPortalModule>()
        };

        foreach (var m in toRemove)
            _moduleEs.Remove(m);

        if (toRemove.Count > 0)
            await _uow.SaveChangesAsync();

        return (true, toRemove.Count, null);
    }

    // ═══ Salon Self-Registration ═══

    public async Task<SlnRegisterResponse> SalonRegisterAsync(SlnRegisterRequest request)
    {
        // Validasyon
        if (string.IsNullOrWhiteSpace(request.SalonName))
            return new SlnRegisterResponse { Error = "Salon adi gereklidir." };
        if (string.IsNullOrWhiteSpace(request.UserName))
            return new SlnRegisterResponse { Error = "Kullanici adi gereklidir." };
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return new SlnRegisterResponse { Error = "Sifre en az 6 karakter olmalidir." };

        var userName = request.UserName.Trim().ToLowerInvariant();
        if (await _userEs.ExistsByUsernameAsync(userName))
            return new SlnRegisterResponse { Error = "Bu kullanici adi zaten kullaniliyor." };

        var activePlans = (await _subscriptionFactory.GetPlansAsync())
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.IntervalMonths)
            .ToList();

        SubscriptionPlan? registrationPlan = null;
        if (request.SubscriptionPlanId.HasValue && request.SubscriptionPlanId.Value > 0)
        {
            registrationPlan = activePlans.FirstOrDefault(p => p.Id == request.SubscriptionPlanId.Value);
            if (registrationPlan == null)
                return new SlnRegisterResponse { Error = "Secilen abonelik plani bulunamadi." };
        }
        registrationPlan ??= activePlans.FirstOrDefault();

        var registeredAt = DateTime.UtcNow;
        var trialEndsAt = registeredAt.AddDays(PlatformBillingAccessPolicy.RegistrationTrialDays);

        // Customer (Salon)
        var customer = new Customer
        {
            Name = request.SalonName.Trim(),
            Phone = request.Phone,
            Email = request.Email,
            MaxUsers = 5,
            IsActive = true,
            CreatedAt = registeredAt,
            BillingAnchorDay = trialEndsAt.Day
        };
        _customerEs.Add(customer);
        await _uow.SaveChangesAsync();

        var hqBranch = new SlnBranch
        {
            CustomerId = customer.Id,
            Name = "Merkez",
            Slug = await CreateUniqueBranchSlugAsync(customer.Name),
            IsHeadquarter = true,
            IsActive = true,
            ActivatedAt = registeredAt,
            CompanyTitle = customer.Name,
            Phone = request.Phone,
            Email = request.Email
        };
        _branchEs.Add(hqBranch);
        await _uow.SaveChangesAsync();

        // Salon urunu ekle
        _customerProductEs.Add(new CustomerProduct
        {
            CustomerId = customer.Id,
            ProductTypeId = ProductTypes.Ids.Salon,
            MonthlyPrice = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _uow.SaveChangesAsync();

        // Salon portal modullerini ekle
        foreach (var module in SalonPortalModules.All)
        {
            _moduleEs.Add(new CustomerPortalModule
            {
                CustomerId = customer.Id,
                ModuleId = module.Id,
                IsActive = module.IsDefault,
                ActivatedAt = DateTime.UtcNow
            });
        }
        await _uow.SaveChangesAsync();

        // User + Personnel (SalonOwner)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User
        {
            UserName = userName,
            FullName = request.OwnerFullName.Trim(),
            Email = request.Email,
            PasswordHash = passwordHash,
            RoleId = UserRoles.Ids.CustomerUser,
            StatusId = AgentStatuses.Ids.Offline,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PasswordChangedAt = DateTime.UtcNow
        };
        _userEs.Add(user);
        await _uow.SaveChangesAsync();

        await _passwordPolicy.RecordPasswordAsync(user.Id, passwordHash);

        var personnel = new CustomerPersonnel
        {
            UserId = user.Id,
            CustomerId = customer.Id,
            Title = "Salon Sahibi",
            CustomerRoleId = SalonRoles.Ids.SalonOwner,
            IsCustomerAdmin = true,
            IsActive = true,
            BranchId = hqBranch.Id,
            CreatedAt = DateTime.UtcNow
        };
        _personnelEs.Add(personnel);
        await _uow.SaveChangesAsync();

        // Default salon verileri (hizmet kategorileri, hizmetler, masraf kategorileri, kasa)
        await SalonDefaultDataHelper.SeedDefaultDataAsync(_serviceCategoryEs, _serviceEs, _expenseCategoryEs, _cashRegisterEs, _moduleEs, _branchEs, _uow, customer.Id);

        // Deneme: StartDate demo bitisidir. Kayit aninda tahakkuk olusturulmaz;
        // ilk donem odeme baslatildiginda abonelikten turetilir.
        try
        {
            if (registrationPlan != null)
            {
                var subscriptionStart = DateTime.SpecifyKind(trialEndsAt, DateTimeKind.Utc);
                _subscriptionEs.Add(new CustomerSubscription
                {
                    CustomerId = customer.Id,
                    PlanId = registrationPlan.Id,
                    StartDate = subscriptionStart,
                    MonthlyPrice = 0,
                    PeriodPrice = 0,
                    BillingDay = subscriptionStart.Day,
                    StatusId = 1, // Active (trial)
                    PaymentGraceDays = 5
                });
                await _uow.SaveChangesAsync();
                try
                {
                    await _subscriptionFactory.RefreshSubscriptionDisplayMonthlyPriceAsync(customer.Id);
                }
                catch
                {
                    /* Ozet tutari guncellenmezse kayit akisi surer */
                }
            }
            // Plan yoksa: IsTest dokunulmaz (false). Test muafiyeti sadece admin firma detayinda isaretlenir.
        }
        catch { /* trial olusturulamazsa kayit akisini bozma */ }

        // Otomatik login
        var (success, loginResponse, error) = await _authFactory.LoginAsync(new LoginRequest
        {
            UserName = userName,
            Password = request.Password
        });

        if (!success)
            return new SlnRegisterResponse { Success = true, Error = "Kayit basarili ama otomatik giris yapilamadi. Lutfen giris yapin." };

        return new SlnRegisterResponse
        {
            Success = true,
            Token = loginResponse!.Token,
            RefreshToken = loginResponse.RefreshToken,
            FullName = loginResponse.FullName,
            Role = loginResponse.Role
        };
    }

    private async Task<string> CreateUniqueBranchSlugAsync(string input)
    {
        var baseSlug = GenerateSlug(input);
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "salon";

        var slug = baseSlug;
        var suffix = 2;
        while (await _branchEs.GetAllQueryable().AnyAsync(b => b.Slug == slug))
        {
            slug = $"{baseSlug}-{suffix++}";
        }
        return slug;
    }

    private static string GenerateSlug(string input)
    {
        var slug = (input ?? "").ToLowerInvariant().Trim();
        slug = slug.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                   .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s-]+", "-").Trim('-');
        return slug;
    }
}

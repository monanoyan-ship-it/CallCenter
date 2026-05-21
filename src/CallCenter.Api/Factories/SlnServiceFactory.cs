using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnServiceFactory : ISlnServiceFactory
{
    private readonly ISlnServiceCategoryEntityService _categories;
    private readonly ISlnServiceEntityService _services;
    private readonly ISlnResourceEntityService _resources;
    private readonly ISlnServiceResourceRequirementEntityService _requirements;
    private readonly ISlnServiceComboEntityService _combos;
    private readonly ISlnServiceComboItemEntityService _comboItems;
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnBranchEntityService _branches;
    private readonly ISlnAppointmentServiceEntityService _appointmentServices;
    private readonly ISlnInvoiceItemEntityService _invoiceItems;
    private readonly ISlnPackageDefinitionEntityService _packageDefinitions;
    private readonly ISlnRecipeEntityService _recipes;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnServiceFactory> _logger;

    public SlnServiceFactory(
        ISlnServiceCategoryEntityService categories,
        ISlnServiceEntityService services,
        ISlnResourceEntityService resources,
        ISlnServiceResourceRequirementEntityService requirements,
        ISlnServiceComboEntityService combos,
        ISlnServiceComboItemEntityService comboItems,
        ISlnAppointmentEntityService appointments,
        ISlnBranchEntityService branches,
        ISlnAppointmentServiceEntityService appointmentServices,
        ISlnInvoiceItemEntityService invoiceItems,
        ISlnPackageDefinitionEntityService packageDefinitions,
        ISlnRecipeEntityService recipes,
        IUnitOfWork uow,
        ILogger<SlnServiceFactory> logger)
    {
        _categories = categories;
        _services = services;
        _resources = resources;
        _requirements = requirements;
        _combos = combos;
        _comboItems = comboItems;
        _appointments = appointments;
        _branches = branches;
        _appointmentServices = appointmentServices;
        _invoiceItems = invoiceItems;
        _packageDefinitions = packageDefinitions;
        _recipes = recipes;
        _uow = uow;
        _logger = logger;
    }

    public async Task<List<SlnServiceCategoryDto>> GetCategoriesWithServicesAsync(int customerId)
    {
        var categories = await _categories.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .Include(c => c.Services).ThenInclude(s => s.ResourceRequirements).ThenInclude(r => r.Resource)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return categories.Select(c => new SlnServiceCategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            IconClass = c.IconClass,
            Color = c.Color,
            SortOrder = c.SortOrder,
            IsActive = c.IsActive,
            Services = c.Services.OrderBy(s => s.SortOrder).Select(s => MapServiceToDto(s, c.Name)).ToList()
        }).ToList();
    }

    public async Task<SlnServiceCategoryDto> CreateCategoryAsync(string name, int sortOrder, int customerId, string? iconClass = null, string? color = null, bool isActive = true)
    {
        var category = new SlnServiceCategory
        {
            CustomerId = customerId,
            Name = name,
            IconClass = NormalizeOptional(iconClass),
            Color = NormalizeOptional(color),
            SortOrder = sortOrder,
            IsActive = isActive
        };

        _categories.Add(category);
        await _uow.SaveChangesAsync();

        return new SlnServiceCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            IconClass = category.IconClass,
            Color = category.Color,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        };
    }

    public async Task<(bool Success, string? Error)> UpdateCategoryAsync(int categoryId, string name, int sortOrder, bool? isActive, int customerId, string? iconClass = null, string? color = null)
    {
        var category = await _categories.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.CustomerId == customerId);

        if (category == null) return (false, "Kategori bulunamadı");

        category.Name = name;
        category.SortOrder = sortOrder;
        if (isActive.HasValue)
            category.IsActive = isActive.Value;
        if (iconClass != null) category.IconClass = NormalizeOptional(iconClass);
        if (color != null) category.Color = NormalizeOptional(color);

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCategoryAsync(int categoryId, int customerId)
    {
        var category = await _categories.GetAllQueryable()
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.CustomerId == customerId);

        if (category == null) return (false, "Kategori bulunamadı");
        if (category.Services.Any()) return (false, "Kategoride hizmet bulunuyor, önce hizmetleri silin");

        _categories.Remove(category);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<SlnServiceDto>> GetServicesAsync(int customerId, int? categoryId = null)
    {
        var query = _services.GetAllQueryable()
            .Where(s => s.CustomerId == customerId);

        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);

        var services = await query
            .Include(s => s.Category)
            .Include(s => s.ResourceRequirements).ThenInclude(r => r.Resource)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        return services.Select(s => MapServiceToDto(s, s.Category?.Name ?? "")).ToList();
    }

    public async Task<(SlnServiceDto? Service, string? Error)> CreateServiceAsync(SlnServiceCreateDto dto, int customerId)
    {
        var validationError = await ValidateServiceSaveAsync(dto, customerId);
        if (validationError != null)
            return (null, validationError);

        var service = new SlnService
        {
            CustomerId = customerId,
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            DurationMinutes = dto.DurationMinutes,
            BufferBeforeMinutes = Math.Max(0, dto.BufferBeforeMinutes),
            BufferAfterMinutes = Math.Max(0, dto.BufferAfterMinutes),
            ProcessingMinutes = Math.Max(0, dto.ProcessingMinutes),
            Price = dto.Price,
            TaxRate = dto.TaxRate ?? 10,
            SortOrder = dto.SortOrder ?? 0,
            ParentServiceId = dto.ParentServiceId,
            IsAddOn = dto.IsAddOn,
            RequiresConsultation = dto.RequiresConsultation,
            RequiresPatchTest = dto.RequiresPatchTest,
            PrerequisiteNotes = dto.PrerequisiteNotes
        };

        _services.Add(service);
        await _uow.SaveChangesAsync();

        var syncResult = await SyncRequirementsAsync(service.Id, dto.ResourceRequirements, customerId);
        if (!syncResult.Success)
        {
            _services.Remove(service);
            await _uow.SaveChangesAsync();
            return (null, syncResult.Error);
        }

        await _uow.SaveChangesAsync();
        return ((await GetServicesAsync(customerId)).First(s => s.Id == service.Id), null);
    }

    public async Task<(bool Success, string? Error)> UpdateServiceAsync(int serviceId, SlnServiceCreateDto dto, bool? isActive, int customerId, bool syncResourceRequirements = true)
    {
        var service = await _services.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.CustomerId == customerId);

        if (service == null) return (false, "Hizmet bulunamadı");

        var validationError = await ValidateServiceSaveAsync(dto, customerId, serviceId, syncResourceRequirements);
        if (validationError != null)
            return (false, validationError);

        service.CategoryId = dto.CategoryId;
        service.Name = dto.Name;
        service.DurationMinutes = dto.DurationMinutes;
        service.BufferBeforeMinutes = Math.Max(0, dto.BufferBeforeMinutes);
        service.BufferAfterMinutes = Math.Max(0, dto.BufferAfterMinutes);
        service.ProcessingMinutes = Math.Max(0, dto.ProcessingMinutes);
        service.Price = dto.Price;
        if (dto.TaxRate.HasValue)
            service.TaxRate = dto.TaxRate.Value;
        if (dto.SortOrder.HasValue)
            service.SortOrder = dto.SortOrder.Value;
        service.ParentServiceId = dto.ParentServiceId;
        service.IsAddOn = dto.IsAddOn;
        service.RequiresConsultation = dto.RequiresConsultation;
        service.RequiresPatchTest = dto.RequiresPatchTest;
        service.PrerequisiteNotes = dto.PrerequisiteNotes;
        if (isActive.HasValue)
            service.IsActive = isActive.Value;

        if (syncResourceRequirements)
        {
            var syncResult = await SyncRequirementsAsync(service.Id, dto.ResourceRequirements, customerId);
            if (!syncResult.Success)
                return (false, syncResult.Error);
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteServiceAsync(int serviceId, int customerId)
    {
        var service = await _services.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.CustomerId == customerId);

        if (service == null) return (false, "Hizmet bulunamadı");

        if (await HasServiceReferencesAsync(serviceId, customerId))
        {
            service.IsActive = false;
            await _uow.SaveChangesAsync();
            return (true, null);
        }

        _services.Remove(service);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private async Task<bool> HasServiceReferencesAsync(int serviceId, int customerId)
    {
        if (await _appointments.GetAllQueryable()
            .AnyAsync(a => a.CustomerId == customerId && a.ServiceId == serviceId))
            return true;

        if (await _appointmentServices.GetAllQueryable()
            .Include(s => s.SlnAppointment)
            .AnyAsync(s => s.SlnServiceId == serviceId && s.SlnAppointment != null && s.SlnAppointment.CustomerId == customerId))
            return true;

        if (await _invoiceItems.GetAllQueryable()
            .Include(i => i.Invoice)
            .AnyAsync(i => i.ServiceId == serviceId && i.Invoice != null && i.Invoice.CustomerId == customerId))
            return true;

        if (await _packageDefinitions.GetAllQueryable()
            .AnyAsync(d => d.CustomerId == customerId && d.ServiceId == serviceId))
            return true;

        if (await _comboItems.GetAllQueryable()
            .Include(i => i.Combo)
            .AnyAsync(i => i.ServiceId == serviceId && i.Combo != null && i.Combo.CustomerId == customerId))
            return true;

        if (await _recipes.GetAllQueryable()
            .AnyAsync(r => r.CustomerId == customerId && r.ServiceId == serviceId))
            return true;

        return await _services.GetAllQueryable()
            .AnyAsync(s => s.CustomerId == customerId && s.ParentServiceId == serviceId);
    }

    public async Task<List<SlnResourceDto>> GetResourcesAsync(int customerId, int? branchScopeId = null)
    {
        var query = _resources.GetAllQueryable()
            .Where(r => r.CustomerId == customerId);

        if (branchScopeId.HasValue)
            query = query.Where(r => r.BranchId == null || r.BranchId == branchScopeId.Value);

        var resources = await query.Include(r => r.Branch)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
            .ToListAsync();

        return resources.Select(MapResourceToDto).ToList();
    }

    public async Task<SlnResourceDto> CreateResourceAsync(SlnResourceCreateDto dto, int customerId, int? branchScopeId = null)
    {
        var resource = new SlnResource
        {
            CustomerId = customerId,
            BranchId = branchScopeId ?? await NormalizeBranchIdAsync(dto.BranchId, customerId),
            Name = dto.Name,
            ResourceKind = dto.ResourceKind,
            Quantity = Math.Max(1, dto.Quantity),
            IsActive = dto.IsActive,
            SortOrder = dto.SortOrder,
            Notes = dto.Notes
        };

        _resources.Add(resource);
        await _uow.SaveChangesAsync();

        var created = await _resources.GetAllQueryable().Include(r => r.Branch).FirstAsync(r => r.Id == resource.Id);
        return MapResourceToDto(created);
    }

    public async Task<(bool Success, string? Error)> UpdateResourceAsync(int resourceId, SlnResourceCreateDto dto, int customerId, int? branchScopeId = null)
    {
        var resource = await _resources.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.CustomerId == customerId);
        if (resource == null) return (false, "Kaynak bulunamadı");
        if (!ResourceWriteScopeAllows(resource, branchScopeId))
        {
            return (false, "Bu kaynak için yetkiniz yok");
        }

        resource.BranchId = branchScopeId ?? await NormalizeBranchIdAsync(dto.BranchId, customerId);
        resource.Name = dto.Name;
        resource.ResourceKind = dto.ResourceKind;
        resource.Quantity = Math.Max(1, dto.Quantity);
        resource.IsActive = dto.IsActive;
        resource.SortOrder = dto.SortOrder;
        resource.Notes = dto.Notes;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteResourceAsync(int resourceId, int customerId, int? branchScopeId = null)
    {
        var resource = await _resources.GetAllQueryable()
            .Include(r => r.ServiceRequirements)
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.CustomerId == customerId);
        if (resource == null) return (false, "Kaynak bulunamadı");
        if (!ResourceWriteScopeAllows(resource, branchScopeId))
        {
            return (false, "Bu kaynak için yetkiniz yok");
        }
        if (resource.ServiceRequirements.Any()) return (false, "Bu kaynak hizmetlerde kullanılıyor");

        _resources.Remove(resource);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<SlnServiceComboDto>> GetCombosAsync(int customerId)
    {
        var combos = await _combos.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .Include(c => c.Items).ThenInclude(i => i.Service)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync();

        return combos.Select(MapComboToDto).ToList();
    }

    public async Task<(SlnServiceComboDto? Combo, string? Error)> CreateComboAsync(SlnServiceComboCreateDto dto, int customerId)
    {
        var validationError = await ValidateComboSaveAsync(dto, customerId);
        if (validationError != null)
            return (null, validationError);

        var combo = new SlnServiceCombo
        {
            CustomerId = customerId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            IsActive = dto.IsActive,
            SortOrder = dto.SortOrder
        };

        _combos.Add(combo);
        await _uow.SaveChangesAsync();

        var syncResult = await SyncComboItemsAsync(combo.Id, dto.Items, customerId);
        if (!syncResult.Success)
        {
            _combos.Remove(combo);
            await _uow.SaveChangesAsync();
            return (null, syncResult.Error);
        }

        await _uow.SaveChangesAsync();
        return ((await GetCombosAsync(customerId)).First(c => c.Id == combo.Id), null);
    }

    public async Task<(bool Success, string? Error)> UpdateComboAsync(int comboId, SlnServiceComboCreateDto dto, int customerId)
    {
        var combo = await _combos.GetAllQueryable()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == comboId && c.CustomerId == customerId);
        if (combo == null) return (false, "Combo bulunamadı");

        var validationError = await ValidateComboSaveAsync(dto, customerId);
        if (validationError != null)
            return (false, validationError);

        combo.Name = dto.Name;
        combo.Description = dto.Description;
        combo.Price = dto.Price;
        combo.IsActive = dto.IsActive;
        combo.SortOrder = dto.SortOrder;

        var syncResult = await SyncComboItemsAsync(combo.Id, dto.Items, customerId);
        if (!syncResult.Success)
            return (false, syncResult.Error);

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteComboAsync(int comboId, int customerId)
    {
        var combo = await _combos.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == comboId && c.CustomerId == customerId);
        if (combo == null) return (false, "Combo bulunamadı");
        var hasAppointments = await _appointments.GetAllQueryable()
            .AnyAsync(a => a.CustomerId == customerId && a.ComboId == comboId);
        if (hasAppointments) return (false, "Bu combo randevularda kullanılıyor");

        _combos.Remove(combo);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private async Task<(bool Success, string? Error)> SyncRequirementsAsync(int serviceId, List<SlnServiceResourceRequirementCreateDto>? incoming, int customerId)
    {
        var requirements = incoming ?? [];
        var validationError = await ValidateResourceRequirementsAsync(requirements, customerId);
        if (validationError != null)
            return (false, validationError);

        var existing = await _requirements.GetAllQueryable()
            .Where(r => r.ServiceId == serviceId)
            .ToListAsync();
        _requirements.RemoveRange(existing);

        foreach (var item in requirements)
        {
            _requirements.Add(new SlnServiceResourceRequirement
            {
                ServiceId = serviceId,
                ResourceId = item.ResourceId,
                QuantityRequired = Math.Max(1, item.QuantityRequired)
            });
        }

        return (true, null);
    }

    private async Task<string?> ValidateServiceSaveAsync(
        SlnServiceCreateDto dto,
        int customerId,
        int? currentServiceId = null,
        bool validateResourceRequirements = true)
    {
        if (dto.TaxRate.HasValue && (dto.TaxRate.Value < 0 || dto.TaxRate.Value > 100)) return "KDV oranı 0 ile 100 arasında olmalı";
        if (dto.SortOrder.HasValue && dto.SortOrder.Value < 0) return "Sıra 0 veya daha büyük olmalı";

        var categoryExists = await _categories.GetAllQueryable()
            .AnyAsync(c => c.Id == dto.CategoryId && c.CustomerId == customerId);
        if (!categoryExists) return "Kategori bulunamadı";

        if (dto.ParentServiceId.HasValue)
        {
            if (currentServiceId.HasValue && dto.ParentServiceId.Value == currentServiceId.Value)
                return "Hizmet kendisinin üst hizmeti olamaz";

            var parentExists = await _services.GetAllQueryable()
                .AnyAsync(s => s.Id == dto.ParentServiceId.Value && s.CustomerId == customerId);
            if (!parentExists) return "Üst hizmet bulunamadı";
        }

        return validateResourceRequirements
            ? await ValidateResourceRequirementsAsync(dto.ResourceRequirements, customerId)
            : null;
    }

    private async Task<string?> ValidateResourceRequirementsAsync(List<SlnServiceResourceRequirementCreateDto>? incoming, int customerId)
    {
        var requirements = incoming ?? [];
        var resourceIds = requirements
            .Select(i => i.ResourceId)
            .Distinct()
            .ToList();
        if (resourceIds.Count == 0) return null;

        var validCount = await _resources.GetAllQueryable()
            .CountAsync(r => r.CustomerId == customerId && resourceIds.Contains(r.Id));

        return validCount == resourceIds.Count ? null : "Kaynak bulunamadı";
    }

    private async Task<(bool Success, string? Error)> SyncComboItemsAsync(int comboId, List<SlnServiceComboItemCreateDto>? incoming, int customerId)
    {
        var items = incoming ?? [];
        var validationError = await ValidateComboItemsAsync(items, customerId);
        if (validationError != null)
            return (false, validationError);

        var existing = await _comboItems.GetAllQueryable()
            .Where(i => i.ComboId == comboId)
            .ToListAsync();
        _comboItems.RemoveRange(existing);

        var sortOrder = 1;
        foreach (var item in items)
        {
            _comboItems.Add(new SlnServiceComboItem
            {
                ComboId = comboId,
                ServiceId = item.ServiceId,
                SortOrder = item.SortOrder > 0 ? item.SortOrder : sortOrder++
            });
        }

        return (true, null);
    }

    private async Task<string?> ValidateComboSaveAsync(SlnServiceComboCreateDto dto, int customerId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return "Combo adı zorunlu";
        if (dto.Price < 0) return "Fiyat 0 veya daha büyük olmalı";

        return await ValidateComboItemsAsync(dto.Items, customerId);
    }

    private async Task<string?> ValidateComboItemsAsync(List<SlnServiceComboItemCreateDto>? incoming, int customerId)
    {
        var items = incoming ?? [];
        var serviceIds = items
            .Select(i => i.ServiceId)
            .Distinct()
            .ToList();
        if (serviceIds.Count == 0) return "Combo için en az bir hizmet seçin";
        if (serviceIds.Count != items.Count) return "Combo hizmetleri tekrar etmemeli";

        var validCount = await _services.GetAllQueryable()
            .CountAsync(s => s.CustomerId == customerId && serviceIds.Contains(s.Id));

        return validCount == serviceIds.Count ? null : "Hizmet bulunamadı";
    }

    private async Task<int?> NormalizeBranchIdAsync(int? branchId, int customerId)
    {
        if (!branchId.HasValue || branchId.Value <= 0)
        {
            return null;
        }

        var exists = await _branches.GetAllQueryable()
            .AnyAsync(b => b.Id == branchId.Value && b.CustomerId == customerId && b.IsActive);

        return exists ? branchId.Value : null;
    }

    private static bool ResourceWriteScopeAllows(SlnResource resource, int? branchScopeId)
        => !branchScopeId.HasValue || resource.BranchId == branchScopeId.Value;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SlnServiceDto MapServiceToDto(SlnService s, string categoryName) => new()
    {
        Id = s.Id,
        CategoryId = s.CategoryId,
        CategoryName = categoryName,
        Name = s.Name,
        DurationMinutes = s.DurationMinutes,
        BufferBeforeMinutes = s.BufferBeforeMinutes,
        BufferAfterMinutes = s.BufferAfterMinutes,
        ProcessingMinutes = s.ProcessingMinutes,
        Price = s.Price,
        ParentServiceId = s.ParentServiceId,
        TaxRate = s.TaxRate,
        SortOrder = s.SortOrder,
        IsAddOn = s.IsAddOn,
        RequiresConsultation = s.RequiresConsultation,
        RequiresPatchTest = s.RequiresPatchTest,
        PrerequisiteNotes = s.PrerequisiteNotes,
        IsActive = s.IsActive,
        ResourceRequirements = s.ResourceRequirements.OrderBy(r => r.Resource?.Name).Select(r => new SlnServiceResourceRequirementDto
        {
            Id = r.Id,
            ResourceId = r.ResourceId,
            ResourceName = r.Resource?.Name ?? "",
            QuantityRequired = r.QuantityRequired
        }).ToList()
    };

    private static SlnResourceDto MapResourceToDto(SlnResource r) => new()
    {
        Id = r.Id,
        BranchId = r.BranchId,
        BranchName = r.Branch?.Name,
        Name = r.Name,
        ResourceKind = r.ResourceKind,
        Quantity = r.Quantity,
        IsActive = r.IsActive,
        SortOrder = r.SortOrder,
        Notes = r.Notes
    };

    private static SlnServiceComboDto MapComboToDto(SlnServiceCombo combo) => new()
    {
        Id = combo.Id,
        Name = combo.Name,
        Description = combo.Description,
        Price = combo.Price,
        DurationMinutes = combo.Items.Sum(i => (i.Service?.DurationMinutes ?? 0)
            + (i.Service?.BufferBeforeMinutes ?? 0)
            + (i.Service?.BufferAfterMinutes ?? 0)),
        IsActive = combo.IsActive,
        SortOrder = combo.SortOrder,
        Items = combo.Items.OrderBy(i => i.SortOrder).Select(i => new SlnServiceComboItemDto
        {
            Id = i.Id,
            ServiceId = i.ServiceId,
            ServiceName = i.Service?.Name ?? "",
            DurationMinutes = (i.Service?.DurationMinutes ?? 0)
                + (i.Service?.BufferBeforeMinutes ?? 0)
                + (i.Service?.BufferAfterMinutes ?? 0),
            SortOrder = i.SortOrder
        }).ToList()
    };
}

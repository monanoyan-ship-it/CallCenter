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
    private readonly ISlnBranchEntityService _branches;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnServiceFactory> _logger;

    public SlnServiceFactory(
        ISlnServiceCategoryEntityService categories,
        ISlnServiceEntityService services,
        ISlnResourceEntityService resources,
        ISlnServiceResourceRequirementEntityService requirements,
        ISlnServiceComboEntityService combos,
        ISlnServiceComboItemEntityService comboItems,
        ISlnBranchEntityService branches,
        IUnitOfWork uow,
        ILogger<SlnServiceFactory> logger)
    {
        _categories = categories;
        _services = services;
        _resources = resources;
        _requirements = requirements;
        _combos = combos;
        _comboItems = comboItems;
        _branches = branches;
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

    public async Task<SlnServiceCategoryDto> CreateCategoryAsync(string name, int sortOrder, int customerId)
    {
        var category = new SlnServiceCategory
        {
            CustomerId = customerId,
            Name = name,
            SortOrder = sortOrder
        };

        _categories.Add(category);
        await _uow.SaveChangesAsync();

        return new SlnServiceCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        };
    }

    public async Task<(bool Success, string? Error)> UpdateCategoryAsync(int categoryId, string name, int sortOrder, bool isActive, int customerId)
    {
        var category = await _categories.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.CustomerId == customerId);

        if (category == null) return (false, "Kategori bulunamadi");

        category.Name = name;
        category.SortOrder = sortOrder;
        category.IsActive = isActive;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCategoryAsync(int categoryId, int customerId)
    {
        var category = await _categories.GetAllQueryable()
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.CustomerId == customerId);

        if (category == null) return (false, "Kategori bulunamadi");
        if (category.Services.Any()) return (false, "Kategoride hizmet bulunuyor, once hizmetleri silin");

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

    public async Task<SlnServiceDto> CreateServiceAsync(SlnServiceCreateDto dto, int customerId)
    {
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
            ParentServiceId = dto.ParentServiceId,
            IsAddOn = dto.IsAddOn,
            RequiresConsultation = dto.RequiresConsultation,
            RequiresPatchTest = dto.RequiresPatchTest,
            PrerequisiteNotes = dto.PrerequisiteNotes
        };

        _services.Add(service);
        await _uow.SaveChangesAsync();
        await SyncRequirementsAsync(service.Id, dto.ResourceRequirements, customerId);

        return (await GetServicesAsync(customerId)).First(s => s.Id == service.Id);
    }

    public async Task<(bool Success, string? Error)> UpdateServiceAsync(int serviceId, SlnServiceCreateDto dto, bool isActive, int customerId)
    {
        var service = await _services.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.CustomerId == customerId);

        if (service == null) return (false, "Hizmet bulunamadi");

        service.CategoryId = dto.CategoryId;
        service.Name = dto.Name;
        service.DurationMinutes = dto.DurationMinutes;
        service.BufferBeforeMinutes = Math.Max(0, dto.BufferBeforeMinutes);
        service.BufferAfterMinutes = Math.Max(0, dto.BufferAfterMinutes);
        service.ProcessingMinutes = Math.Max(0, dto.ProcessingMinutes);
        service.Price = dto.Price;
        service.ParentServiceId = dto.ParentServiceId;
        service.IsAddOn = dto.IsAddOn;
        service.RequiresConsultation = dto.RequiresConsultation;
        service.RequiresPatchTest = dto.RequiresPatchTest;
        service.PrerequisiteNotes = dto.PrerequisiteNotes;
        service.IsActive = isActive;

        await SyncRequirementsAsync(service.Id, dto.ResourceRequirements, customerId);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteServiceAsync(int serviceId, int customerId)
    {
        var service = await _services.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.CustomerId == customerId);

        if (service == null) return (false, "Hizmet bulunamadi");

        _services.Remove(service);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<SlnResourceDto>> GetResourcesAsync(int customerId)
    {
        var resources = await _resources.GetAllQueryable()
            .Where(r => r.CustomerId == customerId)
            .Include(r => r.Branch)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
            .ToListAsync();

        return resources.Select(MapResourceToDto).ToList();
    }

    public async Task<SlnResourceDto> CreateResourceAsync(SlnResourceCreateDto dto, int customerId)
    {
        var branchId = await NormalizeBranchIdAsync(dto.BranchId, customerId);
        var resource = new SlnResource
        {
            CustomerId = customerId,
            BranchId = branchId,
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

    public async Task<(bool Success, string? Error)> UpdateResourceAsync(int resourceId, SlnResourceCreateDto dto, int customerId)
    {
        var resource = await _resources.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.CustomerId == customerId);
        if (resource == null) return (false, "Kaynak bulunamadi");

        resource.BranchId = await NormalizeBranchIdAsync(dto.BranchId, customerId);
        resource.Name = dto.Name;
        resource.ResourceKind = dto.ResourceKind;
        resource.Quantity = Math.Max(1, dto.Quantity);
        resource.IsActive = dto.IsActive;
        resource.SortOrder = dto.SortOrder;
        resource.Notes = dto.Notes;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteResourceAsync(int resourceId, int customerId)
    {
        var resource = await _resources.GetAllQueryable()
            .Include(r => r.ServiceRequirements)
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.CustomerId == customerId);
        if (resource == null) return (false, "Kaynak bulunamadi");
        if (resource.ServiceRequirements.Any()) return (false, "Bu kaynak hizmetlerde kullaniliyor");

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

    public async Task<SlnServiceComboDto> CreateComboAsync(SlnServiceComboCreateDto dto, int customerId)
    {
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
        await SyncComboItemsAsync(combo.Id, dto.Items, customerId);

        return (await GetCombosAsync(customerId)).First(c => c.Id == combo.Id);
    }

    public async Task<(bool Success, string? Error)> UpdateComboAsync(int comboId, SlnServiceComboCreateDto dto, int customerId)
    {
        var combo = await _combos.GetAllQueryable()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == comboId && c.CustomerId == customerId);
        if (combo == null) return (false, "Combo bulunamadi");

        combo.Name = dto.Name;
        combo.Description = dto.Description;
        combo.Price = dto.Price;
        combo.IsActive = dto.IsActive;
        combo.SortOrder = dto.SortOrder;

        await SyncComboItemsAsync(combo.Id, dto.Items, customerId);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteComboAsync(int comboId, int customerId)
    {
        var combo = await _combos.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == comboId && c.CustomerId == customerId);
        if (combo == null) return (false, "Combo bulunamadi");

        _combos.Remove(combo);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private async Task SyncRequirementsAsync(int serviceId, List<SlnServiceResourceRequirementCreateDto> incoming, int customerId)
    {
        var existing = await _requirements.GetAllQueryable()
            .Where(r => r.ServiceId == serviceId)
            .ToListAsync();
        _requirements.RemoveRange(existing);

        var resourceIds = incoming.Select(i => i.ResourceId).Distinct().ToList();
        var validResourceIds = await _resources.GetAllQueryable()
            .Where(r => r.CustomerId == customerId && resourceIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();

        foreach (var item in incoming.Where(i => validResourceIds.Contains(i.ResourceId)))
        {
            _requirements.Add(new SlnServiceResourceRequirement
            {
                ServiceId = serviceId,
                ResourceId = item.ResourceId,
                QuantityRequired = Math.Max(1, item.QuantityRequired)
            });
        }

        await _uow.SaveChangesAsync();
    }

    private async Task SyncComboItemsAsync(int comboId, List<SlnServiceComboItemCreateDto> incoming, int customerId)
    {
        var existing = await _comboItems.GetAllQueryable()
            .Where(i => i.ComboId == comboId)
            .ToListAsync();
        _comboItems.RemoveRange(existing);

        var serviceIds = incoming.Select(i => i.ServiceId).Distinct().ToList();
        var validServiceIds = await _services.GetAllQueryable()
            .Where(s => s.CustomerId == customerId && serviceIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var sortOrder = 1;
        foreach (var item in incoming.Where(i => validServiceIds.Contains(i.ServiceId)))
        {
            _comboItems.Add(new SlnServiceComboItem
            {
                ComboId = comboId,
                ServiceId = item.ServiceId,
                SortOrder = item.SortOrder > 0 ? item.SortOrder : sortOrder++
            });
        }

        await _uow.SaveChangesAsync();
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

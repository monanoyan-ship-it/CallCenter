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
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnServiceFactory> _logger;

    public SlnServiceFactory(
        ISlnServiceCategoryEntityService categories,
        ISlnServiceEntityService services,
        IUnitOfWork uow,
        ILogger<SlnServiceFactory> logger)
    {
        _categories = categories;
        _services = services;
        _uow = uow;
        _logger = logger;
    }

    public async Task<List<SlnServiceCategoryDto>> GetCategoriesWithServicesAsync(int customerId)
    {
        var categories = await _categories.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .Include(c => c.Services)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return categories.Select(c => new SlnServiceCategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            SortOrder = c.SortOrder,
            IsActive = c.IsActive,
            Services = c.Services.OrderBy(s => s.SortOrder).Select(s => new SlnServiceDto
            {
                Id = s.Id,
                CategoryId = s.CategoryId,
                CategoryName = c.Name,
                Name = s.Name,
                DurationMinutes = s.DurationMinutes,
                Price = s.Price,
                IsActive = s.IsActive
            }).ToList()
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
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        return services.Select(s => new SlnServiceDto
        {
            Id = s.Id,
            CategoryId = s.CategoryId,
            CategoryName = s.Category?.Name ?? "",
            Name = s.Name,
            DurationMinutes = s.DurationMinutes,
            Price = s.Price,
            IsActive = s.IsActive
        }).ToList();
    }

    public async Task<SlnServiceDto> CreateServiceAsync(SlnServiceCreateDto dto, int customerId)
    {
        var service = new SlnService
        {
            CustomerId = customerId,
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            DurationMinutes = dto.DurationMinutes,
            Price = dto.Price
        };

        _services.Add(service);
        await _uow.SaveChangesAsync();

        var category = await _categories.GetByIdAsync(dto.CategoryId);

        return new SlnServiceDto
        {
            Id = service.Id,
            CategoryId = service.CategoryId,
            CategoryName = category?.Name ?? "",
            Name = service.Name,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            IsActive = service.IsActive
        };
    }

    public async Task<(bool Success, string? Error)> UpdateServiceAsync(int serviceId, SlnServiceCreateDto dto, bool isActive, int customerId)
    {
        var service = await _services.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.CustomerId == customerId);

        if (service == null) return (false, "Hizmet bulunamadi");

        service.CategoryId = dto.CategoryId;
        service.Name = dto.Name;
        service.DurationMinutes = dto.DurationMinutes;
        service.Price = dto.Price;
        service.IsActive = isActive;

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
}

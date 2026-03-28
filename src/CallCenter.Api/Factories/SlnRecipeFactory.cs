using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnRecipeFactory : ISlnRecipeFactory
{
    private readonly ISlnRecipeEntityService _recipes;
    private readonly ISlnRecipeItemEntityService _recipeItems;
    private readonly ISlnServiceEntityService _services;
    private readonly IUnitOfWork _uow;

    public SlnRecipeFactory(
        ISlnRecipeEntityService recipes,
        ISlnRecipeItemEntityService recipeItems,
        ISlnServiceEntityService services,
        IUnitOfWork uow)
    {
        _recipes = recipes;
        _recipeItems = recipeItems;
        _services = services;
        _uow = uow;
    }

    public async Task<List<SlnRecipeDto>> GetRecipesAsync(int customerId)
    {
        var recipes = await _recipes.GetAllQueryable()
            .Where(r => r.CustomerId == customerId)
            .Include(r => r.Items).ThenInclude(i => i.Service)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return recipes.Select(MapToDto).ToList();
    }

    public async Task<SlnRecipeDto?> GetRecipeAsync(int id, int customerId)
    {
        var recipe = await _recipes.GetAllQueryable()
            .Include(r => r.Items).ThenInclude(i => i.Service)
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);

        return recipe != null ? MapToDto(recipe) : null;
    }

    public async Task<SlnRecipeDto> CreateRecipeAsync(SlnRecipeCreateDto dto, int customerId)
    {
        var serviceIds = dto.Items.Select(i => i.ServiceId).Distinct().ToList();
        var services = await _services.GetAllQueryable()
            .Where(s => serviceIds.Contains(s.Id) && s.CustomerId == customerId)
            .ToListAsync();

        var recipe = new SlnRecipe
        {
            CustomerId = customerId,
            Name = dto.Name,
            Description = dto.Description,
            IconClass = dto.IconClass,
            IsActive = dto.IsActive
        };

        // Toplam fiyat ve sure hesapla
        decimal totalPrice = 0;
        int totalDuration = 0;
        foreach (var item in dto.Items)
        {
            var svc = services.FirstOrDefault(s => s.Id == item.ServiceId);
            if (svc != null)
            {
                totalPrice += svc.Price * item.Quantity;
                totalDuration += svc.DurationMinutes * item.Quantity;
            }
        }
        recipe.TotalPrice = totalPrice;
        recipe.TotalDurationMinutes = totalDuration;

        _recipes.Add(recipe);
        await _uow.SaveChangesAsync();

        var sortOrder = 1;
        foreach (var item in dto.Items)
        {
            _recipeItems.Add(new SlnRecipeItem
            {
                RecipeId = recipe.Id,
                ServiceId = item.ServiceId,
                Quantity = item.Quantity,
                SortOrder = item.SortOrder > 0 ? item.SortOrder : sortOrder++
            });
        }
        await _uow.SaveChangesAsync();

        return (await GetRecipeAsync(recipe.Id, customerId))!;
    }

    public async Task<(bool Success, string? Error)> UpdateRecipeAsync(int id, SlnRecipeCreateDto dto, int customerId)
    {
        var recipe = await _recipes.GetAllQueryable()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);

        if (recipe == null) return (false, "Recete bulunamadi");

        recipe.Name = dto.Name;
        recipe.Description = dto.Description;
        recipe.IconClass = dto.IconClass;
        recipe.IsActive = dto.IsActive;

        // Eski itemlari sil
        _recipeItems.RemoveRange(recipe.Items);

        // Yeni itemlari ekle + toplam hesapla
        var serviceIds = dto.Items.Select(i => i.ServiceId).Distinct().ToList();
        var services = await _services.GetAllQueryable()
            .Where(s => serviceIds.Contains(s.Id) && s.CustomerId == customerId)
            .ToListAsync();

        decimal totalPrice = 0;
        int totalDuration = 0;
        var sortOrder = 1;
        foreach (var item in dto.Items)
        {
            var svc = services.FirstOrDefault(s => s.Id == item.ServiceId);
            if (svc != null)
            {
                totalPrice += svc.Price * item.Quantity;
                totalDuration += svc.DurationMinutes * item.Quantity;
            }
            _recipeItems.Add(new SlnRecipeItem
            {
                RecipeId = recipe.Id,
                ServiceId = item.ServiceId,
                Quantity = item.Quantity,
                SortOrder = item.SortOrder > 0 ? item.SortOrder : sortOrder++
            });
        }
        recipe.TotalPrice = totalPrice;
        recipe.TotalDurationMinutes = totalDuration;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteRecipeAsync(int id, int customerId)
    {
        var recipe = await _recipes.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);

        if (recipe == null) return (false, "Recete bulunamadi");

        _recipes.Remove(recipe);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static SlnRecipeDto MapToDto(SlnRecipe r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        IconClass = r.IconClass,
        TotalPrice = r.TotalPrice,
        TotalDurationMinutes = r.TotalDurationMinutes,
        IsActive = r.IsActive,
        Items = r.Items.OrderBy(i => i.SortOrder).Select(i => new SlnRecipeItemDto
        {
            Id = i.Id,
            ServiceId = i.ServiceId,
            ServiceName = i.Service?.Name ?? "",
            ServicePrice = i.Service?.Price ?? 0,
            ServiceDurationMinutes = i.Service?.DurationMinutes ?? 0,
            Quantity = i.Quantity,
            SortOrder = i.SortOrder
        }).ToList()
    };
}

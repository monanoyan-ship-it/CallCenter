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
    private readonly ISlnProductEntityService _products;
    private readonly IUnitOfWork _uow;

    public SlnRecipeFactory(
        ISlnRecipeEntityService recipes,
        ISlnRecipeItemEntityService recipeItems,
        ISlnProductEntityService products,
        IUnitOfWork uow)
    {
        _recipes = recipes;
        _recipeItems = recipeItems;
        _products = products;
        _uow = uow;
    }

    public async Task<List<SlnRecipeDto>> GetRecipesAsync(int customerId)
    {
        var recipes = await _recipes.GetAllQueryable()
            .Where(r => r.CustomerId == customerId)
            .Include(r => r.Service)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return recipes.Select(MapToDto).ToList();
    }

    public async Task<SlnRecipeDto?> GetRecipeAsync(int id, int customerId)
    {
        var recipe = await _recipes.GetAllQueryable()
            .Include(r => r.Service)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);

        return recipe != null ? MapToDto(recipe) : null;
    }

    public async Task<SlnRecipeDto> CreateRecipeAsync(SlnRecipeCreateDto dto, int customerId)
    {
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _products.GetAllQueryable()
            .Where(p => productIds.Contains(p.Id) && p.CustomerId == customerId)
            .ToListAsync();

        var recipe = new SlnRecipe
        {
            CustomerId = customerId,
            Name = dto.Name,
            Description = dto.Description,
            IconClass = dto.IconClass,
            ServiceId = dto.ServiceId,
            IsActive = dto.IsActive
        };

        // Malzeme maliyeti hesapla
        decimal totalCost = 0;
        foreach (var item in dto.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product != null)
                totalCost += product.PurchasePrice * item.Quantity;
        }
        recipe.EstimatedCost = totalCost;

        _recipes.Add(recipe);
        await _uow.SaveChangesAsync();

        var sortOrder = 1;
        foreach (var item in dto.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            _recipeItems.Add(new SlnRecipeItem
            {
                RecipeId = recipe.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Cost = (product?.PurchasePrice ?? 0) * item.Quantity,
                Notes = item.Notes,
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

        if (recipe == null) return (false, "Reçete bulunamadı");

        recipe.Name = dto.Name;
        recipe.Description = dto.Description;
        recipe.IconClass = dto.IconClass;
        recipe.ServiceId = dto.ServiceId;
        recipe.IsActive = dto.IsActive;

        // Eski itemlari sil
        _recipeItems.RemoveRange(recipe.Items);

        // Yeni itemlari ekle + maliyet hesapla
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _products.GetAllQueryable()
            .Where(p => productIds.Contains(p.Id) && p.CustomerId == customerId)
            .ToListAsync();

        decimal totalCost = 0;
        var sortOrder = 1;
        foreach (var item in dto.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            var cost = (product?.PurchasePrice ?? 0) * item.Quantity;
            totalCost += cost;
            _recipeItems.Add(new SlnRecipeItem
            {
                RecipeId = recipe.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Cost = cost,
                Notes = item.Notes,
                SortOrder = item.SortOrder > 0 ? item.SortOrder : sortOrder++
            });
        }
        recipe.EstimatedCost = totalCost;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteRecipeAsync(int id, int customerId)
    {
        var recipe = await _recipes.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);

        if (recipe == null) return (false, "Reçete bulunamadı");

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
        ServiceId = r.ServiceId,
        ServiceName = r.Service?.Name,
        EstimatedCost = r.EstimatedCost,
        IsActive = r.IsActive,
        Items = r.Items.OrderBy(i => i.SortOrder).Select(i => new SlnRecipeItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? "",
            Quantity = i.Quantity,
            Unit = i.Unit,
            Cost = i.Cost,
            Notes = i.Notes,
            SortOrder = i.SortOrder
        }).ToList()
    };
}

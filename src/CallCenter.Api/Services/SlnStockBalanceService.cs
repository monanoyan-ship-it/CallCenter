using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class SlnStockBalanceService : ISlnStockBalanceService
{
    private readonly ISlnProductBranchStockEntityService _branchStocks;
    private readonly ISlnBranchEntityService _branches;

    public SlnStockBalanceService(
        ISlnProductBranchStockEntityService branchStocks,
        ISlnBranchEntityService branches)
    {
        _branchStocks = branchStocks;
        _branches = branches;
    }

    public async Task<int?> ResolveBranchIdAsync(int customerId, int? branchId)
    {
        if (branchId.HasValue)
        {
            var exists = await _branches.GetAllQueryable()
                .AnyAsync(b => b.Id == branchId.Value && b.CustomerId == customerId && b.IsActive);
            if (exists)
                return branchId.Value;
        }

        return await _branches.GetAllQueryable()
            .Where(b => b.CustomerId == customerId && b.IsActive)
            .OrderBy(b => b.Id)
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<int, decimal>> GetStockQuantitiesAsync(int customerId, IEnumerable<int> productIds, int? branchId)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, decimal>();

        var query = _branchStocks.GetAllQueryable()
            .Where(s => s.CustomerId == customerId && ids.Contains(s.ProductId));

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId.Value);

        return await query
            .GroupBy(s => s.ProductId)
            .Select(g => new { ProductId = g.Key, StockQuantity = g.Sum(x => x.StockQuantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.StockQuantity);
    }

    public async Task<decimal> GetStockQuantityAsync(int customerId, int productId, int? branchId, decimal fallbackQuantity = 0)
    {
        var quantities = await GetStockQuantitiesAsync(customerId, new[] { productId }, branchId);
        return quantities.TryGetValue(productId, out var quantity) ? quantity : fallbackQuantity;
    }

    public async Task SetStockQuantityAsync(int customerId, int productId, int? branchId, decimal quantity)
    {
        var resolvedBranchId = await ResolveBranchIdAsync(customerId, branchId);
        if (!resolvedBranchId.HasValue)
            return;

        var stock = await _branchStocks.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.CustomerId == customerId
                                   && s.ProductId == productId
                                   && s.BranchId == resolvedBranchId.Value);

        if (stock == null)
        {
            _branchStocks.Add(new SlnProductBranchStock
            {
                CustomerId = customerId,
                ProductId = productId,
                BranchId = resolvedBranchId.Value,
                StockQuantity = quantity,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            stock.StockQuantity = quantity;
            stock.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task<(bool Success, string? Error)> AdjustStockAsync(
        SlnProduct product,
        int customerId,
        int? branchId,
        decimal delta,
        bool preventNegative)
    {
        var resolvedBranchId = await ResolveBranchIdAsync(customerId, branchId);
        if (!resolvedBranchId.HasValue)
            return (false, "Sube bulunamadi");

        var stock = await _branchStocks.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.CustomerId == customerId
                                   && s.ProductId == product.Id
                                   && s.BranchId == resolvedBranchId.Value);

        if (stock == null)
        {
            stock = new SlnProductBranchStock
            {
                CustomerId = customerId,
                ProductId = product.Id,
                BranchId = resolvedBranchId.Value,
                StockQuantity = 0,
                CreatedAt = DateTime.UtcNow
            };
            _branchStocks.Add(stock);
        }

        var nextQuantity = stock.StockQuantity + delta;
        if (preventNegative && nextQuantity < 0)
            return (false, $"Yetersiz stok: {product.Name} (Mevcut: {stock.StockQuantity:0.##} {product.Unit})");

        stock.StockQuantity = nextQuantity;
        stock.UpdatedAt = DateTime.UtcNow;
        return (true, null);
    }

    public async Task SyncProductTotalAsync(SlnProduct product, int customerId)
    {
        product.StockQuantity = await _branchStocks.GetAllQueryable()
            .Where(s => s.CustomerId == customerId && s.ProductId == product.Id)
            .SumAsync(s => s.StockQuantity);
    }
}

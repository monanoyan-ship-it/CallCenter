using CallCenter.Shared.Entities;

namespace CallCenter.Api.Services.Interfaces;

public interface ISlnStockBalanceService
{
    Task<int?> ResolveBranchIdAsync(int customerId, int? branchId);
    Task<Dictionary<int, decimal>> GetStockQuantitiesAsync(int customerId, IEnumerable<int> productIds, int? branchId);
    Task<decimal> GetStockQuantityAsync(int customerId, int productId, int? branchId, decimal fallbackQuantity = 0);
    Task SetStockQuantityAsync(int customerId, int productId, int? branchId, decimal quantity);
    Task<(bool Success, string? Error)> AdjustStockAsync(SlnProduct product, int customerId, int? branchId, decimal delta, bool preventNegative);
    Task SyncProductTotalAsync(SlnProduct product, int customerId);
}

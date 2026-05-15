using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnLoyaltyFactory
{
    Task<SlnLoyaltyConfigDto?> GetConfigAsync(int customerId);
    Task<SlnLoyaltyConfigDto> SaveConfigAsync(SlnLoyaltyConfigUpdateDto dto, int customerId);
    Task<List<SlnClientLoyaltyDto>> GetClientLoyaltiesAsync(int customerId, int? branchId = null);
    Task<SlnClientLoyaltyDto?> GetClientLoyaltyAsync(int slnClientId, int customerId, int? branchId = null);
    Task<List<SlnLoyaltyTransactionDto>> GetTransactionsAsync(int slnClientId, int customerId, int? branchId = null);
    Task EarnPointsAsync(int slnClientId, decimal invoiceAmount, int? invoiceId, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> RedeemPointsAsync(SlnLoyaltyRedeemDto dto, int customerId, int? branchId = null);
}

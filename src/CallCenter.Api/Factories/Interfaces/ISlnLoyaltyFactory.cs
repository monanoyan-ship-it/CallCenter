using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnLoyaltyFactory
{
    Task<SlnLoyaltyConfigDto?> GetConfigAsync(int customerId);
    Task<SlnLoyaltyConfigDto> SaveConfigAsync(SlnLoyaltyConfigUpdateDto dto, int customerId);
    Task<List<SlnClientLoyaltyDto>> GetClientLoyaltiesAsync(int customerId);
    Task<SlnClientLoyaltyDto?> GetClientLoyaltyAsync(int slnClientId, int customerId);
    Task<List<SlnLoyaltyTransactionDto>> GetTransactionsAsync(int slnClientId, int customerId);
    Task EarnPointsAsync(int slnClientId, decimal invoiceAmount, int? invoiceId, int customerId);
    Task<(bool Success, string? Error)> RedeemPointsAsync(SlnLoyaltyRedeemDto dto, int customerId);
}

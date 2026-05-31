using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

// Sadakat Programi (D — punch card). Sadakat Puani (C, ISlnLoyaltyFactory) ile karistirilmaz.
public interface ISlnLoyaltyProgramFactory
{
    // Program tanimlari
    Task<List<SlnLoyaltyProgramDto>> GetProgramsAsync(int customerId, int? branchId = null);
    Task<SlnLoyaltyProgramDto> CreateProgramAsync(SlnLoyaltyProgramCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateProgramAsync(int id, SlnLoyaltyProgramCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteProgramAsync(int id, int customerId);

    // Musteri ilerleme + odul listesi
    Task<List<SlnClientLoyaltyProgressDto>> GetClientProgressAsync(int customerId, int? clientId = null, int? branchId = null);
    Task<List<SlnLoyaltyProgramRewardDto>> GetAvailableRewardsAsync(int customerId, int slnClientId, int? branchId = null);

    // Earn trigger (Salon adisyon create akisindan cagrilir)
    Task EarnFromInvoiceItemsAsync(int customerId, int slnClientId, int? branchId, IEnumerable<int> serviceIds, int? invoiceItemId = null);

    // Redeem (odulu adisyon kalemiyle iliskilendir)
    Task<(bool Success, string? Error)> ApplyRewardAsync(int customerId, int rewardId, int invoiceItemId);

    // Adisyon iptal/iade icin geri al
    Task ReverseEarnsFromInvoiceAsync(int customerId, int invoiceId);
}

using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnGiftCardFactory
{
    Task<List<SlnGiftCardDto>> GetGiftCardsAsync(int customerId, int? branchId = null);
    Task<SlnGiftCardDto?> GetGiftCardAsync(int id, int customerId, int? branchId = null);
    Task<SlnGiftCardDto?> GetGiftCardByCodeAsync(string code, int customerId, int? branchId = null);
    Task<(SlnGiftCardDto? Card, string? Error)> CreateGiftCardAsync(SlnGiftCardCreateDto dto, int userId, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> RedeemGiftCardAsync(SlnGiftCardRedeemDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> DeactivateGiftCardAsync(int id, int customerId, int? branchId = null);
    Task<bool> HasRedemptionForInvoiceAsync(int customerId, int invoiceId);
    Task<(bool Success, string? Error)> ReverseInvoiceRedemptionsAsync(int customerId, int invoiceId);
    Task<(bool Success, string? Error)> CancelGiftCardSaleFromInvoiceAsync(int customerId, string? invoiceNotes);
}

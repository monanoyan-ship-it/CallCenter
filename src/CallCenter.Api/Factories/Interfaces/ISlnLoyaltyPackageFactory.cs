using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

// A — Sadakat Paketi (Prepaid Bundle). Cok seansli hizmet (SlnService.SessionCount) ile karistirilmaz.
public interface ISlnLoyaltyPackageFactory
{
    // Teklif tanimlari (Offer)
    Task<List<SlnLoyaltyPackageOfferDto>> GetOffersAsync(int customerId);
    Task<SlnLoyaltyPackageOfferDto> CreateOfferAsync(SlnLoyaltyPackageOfferCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateOfferAsync(int id, SlnLoyaltyPackageOfferCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteOfferAsync(int id, int customerId);

    // Musteri paketleri (Purchase)
    Task<List<SlnLoyaltyPackagePurchaseDto>> GetPurchasesAsync(int customerId, int? clientId = null, int? branchId = null);
    Task<(SlnLoyaltyPackagePurchaseDto? Purchase, string? Error)> SellPurchaseAsync(SlnLoyaltyPackagePurchaseSellDto dto, int userId, int customerId, int? branchId = null);
    Task<List<SlnLoyaltyPackagePurchaseDto>> CreateLoyaltyPurchasesFromInvoiceAsync(int customerId, int slnClientId, int invoiceId, IEnumerable<SlnLoyaltyPackageSaleLine> lines, int userId, int? branchId = null);
    Task<(bool Success, string? Error)> RedeemSessionAsync(SlnLoyaltyPackageRedeemDto dto, int userId, int customerId, int? branchId = null);
    Task<List<SlnLoyaltyPackageRedemptionDto>> GetRedemptionHistoryAsync(int customerId, int? purchaseId = null, int? branchId = null);
    Task<List<SlnLoyaltyPackageBenefitDto>> GetUsablePurchasesAsync(int customerId, int slnClientId, IEnumerable<int> serviceIds, int? branchId = null);
    Task<(bool Success, string? Error)> RecordRedemptionAsync(int customerId, int purchaseId, int? serviceId, int? slnClientId, int userId, string? notes, int? branchId = null, int? invoiceId = null, int? invoiceItemId = null, int? appointmentId = null);
    Task<(bool Success, string? Error)> ReverseInvoiceRedemptionsAsync(int customerId, int invoiceId);
    Task<(bool Success, string? Error)> CancelPurchaseFromInvoiceAsync(int customerId, string? invoiceNotes);
}

public sealed record SlnLoyaltyPackageSaleLine(int ServiceId, decimal PaidAmount, int Quantity, int? InvoiceItemId = null);

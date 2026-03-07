using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IServiceSubscriptionFactory
{
    // Hizmet Tanımları (TypeDefinition bazlı, DB yok)
    List<ServiceTypeDto> GetAllServices();
    List<ServiceTypeDto> GetPremiumServices();

    // Abonelik CRUD
    Task<List<SubscriptionDto>> GetSubscriptionsByCustomerAsync(int customerId);
    Task<(SubscriptionDto? Result, string? Error)> CreateSubscriptionAsync(SubscriptionCreateDto dto);
    Task<(bool Success, string? Error)> UpdateSubscriptionAsync(Guid uid, SubscriptionUpdateDto dto);
    Task<(bool Success, string? Error)> CancelSubscriptionAsync(Guid uid);

    // Toplu Fiyat Artırma
    Task<(BulkPriceAdjustResultDto? Result, string? Error)> AdjustPricesAsync(BulkPriceAdjustDto dto);

    // Faturalandırma
    Task<int> GenerateServiceBillingAsync(int year, int month);
    Task<List<ServiceBillingItemDto>> GetServiceBillingByCustomerAsync(int customerId, int? year, int? month);
    Task<(bool Success, string? Error)> MarkServiceBillingPaidAsync(int billingItemId);

    // Özet
    Task<ServiceSubscriptionSummaryDto> GetSubscriptionSummaryAsync();
}

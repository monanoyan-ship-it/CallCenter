using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IBillingFactory
{
    Task<List<BillingPeriodDto>> GetByCustomerAsync(int customerId);
    Task<(bool Success, string? Error)> UpdatePeriodAsync(int periodId, BillingPeriodUpdateDto dto);
    Task<(int Created, int Skipped, string? Error)> GenerateBulkAsync(int year, int month);
    Task<(bool IsBlocked, string? Reason)> IsCustomerBlockedByBillingAsync(int customerId);
}

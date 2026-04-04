using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IBillingFactory
{
    Task<List<BillingPeriodDto>> GetByCustomerAsync(int customerId);
    Task<(bool Success, string? Error)> UpdatePeriodAsync(int periodId, BillingPeriodUpdateDto dto);
    Task<(bool Success, string? Error)> DeletePeriodAsync(int periodId);
    Task<(int Created, int Skipped, int SkippedNoAnchor, string? Error)> GenerateBulkAsync(int year, int month);
    Task<(bool IsBlocked, string? Reason)> IsCustomerBlockedByBillingAsync(int customerId);
    Task<(bool Success, string? Error)> CreateManualPeriodAsync(BillingPeriodCreateDto dto);
    Task<List<BillingReportDto>> GetBillingReportAsync(int? year, int? month, int? statusId, int? productTypeId = null);
}

using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnServiceSessionFactory
{
    Task<List<SlnServiceSessionPlanDto>> GetPlansAsync(int customerId, int? clientId = null, int? branchId = null, bool activeOnly = false);
    Task<List<SlnServiceSessionPlanDto>> CreatePlansFromInvoiceAsync(int customerId, int slnClientId, int invoiceId, IEnumerable<SlnServiceSessionPlanSaleLine> lines, int userId, int? branchId = null);
    Task<(SlnServiceSessionRecordDto? Record, string? Error)> RecordSessionAsync(SlnServiceSessionUseDto dto, int userId, int customerId, int? branchId = null);
}

public sealed record SlnServiceSessionPlanSaleLine(int ServiceId, decimal SaleAmount, decimal PaidAmount, int Quantity, int? InvoiceItemId = null);

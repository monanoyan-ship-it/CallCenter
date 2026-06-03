using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IAuditLogFactory
{
    Task<PagedResult<AuditLogListDto>> GetAllAsync(int page, int pageSize, string? category = null, string? action = null, string? search = null, DateTime? dateFrom = null, DateTime? dateTo = null, int? customerId = null);
    Task<AuditLogDetailDto?> GetByIdAsync(long id);
    Task<byte[]> ExportCsvAsync(string? category = null, string? action = null, string? search = null, DateTime? dateFrom = null, DateTime? dateTo = null, int? customerId = null);
    Task<List<string>> GetCategoriesAsync();
    Task<List<string>> GetActionsAsync(string? category = null);
}

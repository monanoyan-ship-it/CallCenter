using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IReportFactory
{
    Task<CallReportResponse> GetCallReportAsync(int? customerId, DateTime? from, DateTime? to, int? directionId, int? statusId, int page, int pageSize);
    Task<AgentReportResponse> GetAgentReportAsync(int? customerId, DateTime? from, DateTime? to, int page, int pageSize);
    Task<QueueReportResponse> GetQueueReportAsync(int? customerId, DateTime? from, DateTime? to, int page, int pageSize);
    Task<List<DailyCallStatsDto>> GetDailyTrendAsync(int? customerId, DateTime? from, DateTime? to);
    Task<List<StatusDistributionDto>> GetStatusDistributionAsync(int? customerId, DateTime? from, DateTime? to);
    Task<byte[]> ExportCallReportCsvAsync(int? customerId, DateTime? from, DateTime? to, int? directionId, int? statusId);
    Task<byte[]> ExportCallReportExcelAsync(int? customerId, DateTime? from, DateTime? to, int? directionId, int? statusId);
    Task<byte[]> ExportAgentReportCsvAsync(int? customerId, DateTime? from, DateTime? to);
    Task<byte[]> ExportAgentReportExcelAsync(int? customerId, DateTime? from, DateTime? to);
}

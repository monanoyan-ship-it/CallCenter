using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface IReportService
{
    Task<CallReportResponse> GetCallReportAsync(int? customerId, DateTime? from, DateTime? to, int? directionId, int? statusId, int page, int pageSize);
    Task<AgentReportResponse> GetAgentReportAsync(int? customerId, DateTime? from, DateTime? to, int page, int pageSize);
}

using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IReportFactory
{
    Task<CallReportResponse> GetCallReportAsync(int? customerId, DateTime? from, DateTime? to, int? directionId, int? statusId, int page, int pageSize);
    Task<AgentReportResponse> GetAgentReportAsync(int? customerId, DateTime? from, DateTime? to, int page, int pageSize);
}

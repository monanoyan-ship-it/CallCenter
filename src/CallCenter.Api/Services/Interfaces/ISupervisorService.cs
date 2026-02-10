using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface ISupervisorService
{
    Task<DashboardResponse> GetDashboardAsync(int? customerId);
    Task<List<QueueLiveDto>> GetQueuesLiveAsync(int? customerId);
    Task<List<CustomerSimpleDto>> GetCustomersAsync();
}

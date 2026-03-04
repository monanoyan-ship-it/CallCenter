using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISupervisorFactory
{
    Task<DashboardResponse> GetDashboardAsync(int? customerId);
    Task<List<QueueLiveDto>> GetQueuesLiveAsync(int? customerId);
    Task<List<CustomerSimpleDto>> GetCustomersAsync();
}

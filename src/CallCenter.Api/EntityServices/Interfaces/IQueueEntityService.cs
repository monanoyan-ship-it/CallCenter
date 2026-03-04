using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IQueueEntityService
{
    IQueryable<Queue> GetAllQueryable();
    Task<Queue?> GetByIdAsync(int id);
    Task<Queue?> GetByIdWithAgentsAsync(int id);
    Task<bool> ExistsByNameAndCustomerAsync(int customerId, string name, int? excludeId = null);
    void Add(Queue entity);
    void Update(Queue entity);
    Task<QueueAgent?> GetQueueAgentAsync(int queueId, int agentId);
    void AddQueueAgent(QueueAgent entity);
    void RemoveQueueAgent(QueueAgent entity);
}

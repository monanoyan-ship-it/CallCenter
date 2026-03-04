using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class QueueEntityService : IQueueEntityService
{
    private readonly AppDbContext _db;

    public QueueEntityService(AppDbContext db) => _db = db;

    public IQueryable<Queue> GetAllQueryable()
        => _db.Queues.AsQueryable();

    public Task<Queue?> GetByIdAsync(int id)
        => _db.Queues.FindAsync(id).AsTask();

    public Task<Queue?> GetByIdWithAgentsAsync(int id)
        => _db.Queues
            .Include(q => q.Customer)
            .Include(q => q.QueueAgents)
                .ThenInclude(qa => qa.Agent)
            .FirstOrDefaultAsync(q => q.Id == id);

    public Task<bool> ExistsByNameAndCustomerAsync(int customerId, string name, int? excludeId = null)
        => excludeId.HasValue
            ? _db.Queues.AnyAsync(q => q.CustomerId == customerId && q.Name == name && q.Id != excludeId.Value)
            : _db.Queues.AnyAsync(q => q.CustomerId == customerId && q.Name == name);

    public void Add(Queue entity) => _db.Queues.Add(entity);
    public void Update(Queue entity) => _db.Queues.Update(entity);

    public Task<QueueAgent?> GetQueueAgentAsync(int queueId, int agentId)
        => _db.QueueAgents.FirstOrDefaultAsync(x => x.QueueId == queueId && x.AgentId == agentId);

    public void AddQueueAgent(QueueAgent entity) => _db.QueueAgents.Add(entity);
    public void RemoveQueueAgent(QueueAgent entity) => _db.QueueAgents.Remove(entity);
}

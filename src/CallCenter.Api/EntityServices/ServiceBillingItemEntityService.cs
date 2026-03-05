using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class ServiceBillingItemEntityService : IServiceBillingItemEntityService
{
    private readonly AppDbContext _db;

    public ServiceBillingItemEntityService(AppDbContext db) => _db = db;

    public Task<ServiceBillingItem?> GetByIdAsync(int id)
        => _db.ServiceBillingItems.FindAsync(id).AsTask();

    public IQueryable<ServiceBillingItem> GetAllQueryable()
        => _db.ServiceBillingItems.AsQueryable();

    public void Add(ServiceBillingItem entity) => _db.ServiceBillingItems.Add(entity);
    public void AddRange(IEnumerable<ServiceBillingItem> entities) => _db.ServiceBillingItems.AddRange(entities);
    public void Update(ServiceBillingItem entity) => _db.ServiceBillingItems.Update(entity);
}

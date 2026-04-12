using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class ServicePricingItemEntityService : IServicePricingItemEntityService
{
    private readonly AppDbContext _db;

    public ServicePricingItemEntityService(AppDbContext db) => _db = db;

    public IQueryable<ServicePricingItem> GetAllQueryable()
        => _db.ServicePricingItems.AsQueryable();

    public Task<ServicePricingItem?> GetByIdAsync(int id)
        => _db.ServicePricingItems.FindAsync(id).AsTask();

    public void Add(ServicePricingItem entity) => _db.ServicePricingItems.Add(entity);
    public void Update(ServicePricingItem entity) => _db.ServicePricingItems.Update(entity);
    public void Remove(ServicePricingItem entity) => _db.ServicePricingItems.Remove(entity);
}

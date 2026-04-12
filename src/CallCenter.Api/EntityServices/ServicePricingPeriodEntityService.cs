using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class ServicePricingPeriodEntityService : IServicePricingPeriodEntityService
{
    private readonly AppDbContext _db;

    public ServicePricingPeriodEntityService(AppDbContext db) => _db = db;

    public IQueryable<ServicePricingPeriod> GetAllQueryable()
        => _db.ServicePricingPeriods.AsQueryable();

    public Task<ServicePricingPeriod?> GetByIdAsync(int id)
        => _db.ServicePricingPeriods.FindAsync(id).AsTask();

    public void Add(ServicePricingPeriod entity) => _db.ServicePricingPeriods.Add(entity);
    public void Update(ServicePricingPeriod entity) => _db.ServicePricingPeriods.Update(entity);
    public void Remove(ServicePricingPeriod entity) => _db.ServicePricingPeriods.Remove(entity);
}

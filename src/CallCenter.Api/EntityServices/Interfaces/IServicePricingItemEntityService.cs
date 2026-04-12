using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IServicePricingItemEntityService
{
    IQueryable<ServicePricingItem> GetAllQueryable();
    Task<ServicePricingItem?> GetByIdAsync(int id);
    void Add(ServicePricingItem entity);
    void Update(ServicePricingItem entity);
    void Remove(ServicePricingItem entity);
}

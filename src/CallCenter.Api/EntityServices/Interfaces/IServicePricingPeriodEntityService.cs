using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IServicePricingPeriodEntityService
{
    IQueryable<ServicePricingPeriod> GetAllQueryable();
    Task<ServicePricingPeriod?> GetByIdAsync(int id);
    void Add(ServicePricingPeriod entity);
    void Update(ServicePricingPeriod entity);
    void Remove(ServicePricingPeriod entity);
}

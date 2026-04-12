using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnRevenueShareEntityService
{
    IQueryable<SlnRevenueShare> GetAllQueryable();
    Task<SlnRevenueShare?> GetByIdAsync(int id);
    void Add(SlnRevenueShare entity);
    void Update(SlnRevenueShare entity);
    void Remove(SlnRevenueShare entity);
}

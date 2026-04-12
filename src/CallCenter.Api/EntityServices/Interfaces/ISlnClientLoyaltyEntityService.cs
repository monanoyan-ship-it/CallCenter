using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnClientLoyaltyEntityService
{
    IQueryable<SlnClientLoyalty> GetAllQueryable();
    Task<SlnClientLoyalty?> GetByIdAsync(int id);
    void Add(SlnClientLoyalty entity);
    void Update(SlnClientLoyalty entity);
    void Remove(SlnClientLoyalty entity);
}

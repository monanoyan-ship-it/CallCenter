using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnServiceCategoryEntityService
{
    IQueryable<SlnServiceCategory> GetAllQueryable();
    Task<SlnServiceCategory?> GetByIdAsync(int id);
    void Add(SlnServiceCategory entity);
    void Update(SlnServiceCategory entity);
    void Remove(SlnServiceCategory entity);
}

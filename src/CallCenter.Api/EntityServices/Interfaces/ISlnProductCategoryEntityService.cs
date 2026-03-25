using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnProductCategoryEntityService
{
    IQueryable<SlnProductCategory> GetAllQueryable();
    Task<SlnProductCategory?> GetByIdAsync(int id);
    void Add(SlnProductCategory entity);
    void Update(SlnProductCategory entity);
    void Remove(SlnProductCategory entity);
}

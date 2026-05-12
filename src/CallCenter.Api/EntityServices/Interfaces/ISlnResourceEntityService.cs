using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnResourceEntityService
{
    IQueryable<SlnResource> GetAllQueryable();
    Task<SlnResource?> GetByIdAsync(int id);
    void Add(SlnResource entity);
    void Update(SlnResource entity);
    void Remove(SlnResource entity);
}

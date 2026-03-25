using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnProductEntityService
{
    IQueryable<SlnProduct> GetAllQueryable();
    Task<SlnProduct?> GetByIdAsync(int id);
    void Add(SlnProduct entity);
    void Update(SlnProduct entity);
    void Remove(SlnProduct entity);
}

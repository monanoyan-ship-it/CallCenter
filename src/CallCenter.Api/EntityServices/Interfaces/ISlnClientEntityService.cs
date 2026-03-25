using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnClientEntityService
{
    IQueryable<SlnClient> GetAllQueryable();
    Task<SlnClient?> GetByIdAsync(int id);
    void Add(SlnClient entity);
    void Update(SlnClient entity);
    void Remove(SlnClient entity);
}

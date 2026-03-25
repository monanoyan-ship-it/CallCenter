using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnServiceEntityService
{
    IQueryable<SlnService> GetAllQueryable();
    Task<SlnService?> GetByIdAsync(int id);
    void Add(SlnService entity);
    void Update(SlnService entity);
    void Remove(SlnService entity);
}

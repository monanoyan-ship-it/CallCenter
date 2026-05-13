using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnAdvanceEntityService
{
    IQueryable<SlnAdvance> GetAllQueryable();
    Task<SlnAdvance?> GetByIdAsync(int id);
    void Add(SlnAdvance entity);
    void Update(SlnAdvance entity);
    void Remove(SlnAdvance entity);
}

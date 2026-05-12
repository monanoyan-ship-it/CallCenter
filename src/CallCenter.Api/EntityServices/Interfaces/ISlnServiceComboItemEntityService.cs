using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnServiceComboItemEntityService
{
    IQueryable<SlnServiceComboItem> GetAllQueryable();
    Task<SlnServiceComboItem?> GetByIdAsync(int id);
    void Add(SlnServiceComboItem entity);
    void Remove(SlnServiceComboItem entity);
    void RemoveRange(IEnumerable<SlnServiceComboItem> entities);
}

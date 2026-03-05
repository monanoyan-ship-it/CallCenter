using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IDataInventoryEntityService
{
    IQueryable<DataInventoryItem> GetAllQueryable();
    Task<DataInventoryItem?> GetByIdAsync(int id);
    void Add(DataInventoryItem entity);
    void Update(DataInventoryItem entity);
    void Remove(DataInventoryItem entity);
}

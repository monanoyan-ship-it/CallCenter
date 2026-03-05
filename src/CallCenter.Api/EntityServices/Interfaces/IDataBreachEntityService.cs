using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IDataBreachEntityService
{
    IQueryable<DataBreach> GetAllQueryable();
    Task<DataBreach?> GetByIdAsync(int id);
    Task<DataBreach?> GetByUidAsync(Guid uid);
    void Add(DataBreach entity);
    void Update(DataBreach entity);
    void Remove(DataBreach entity);
}

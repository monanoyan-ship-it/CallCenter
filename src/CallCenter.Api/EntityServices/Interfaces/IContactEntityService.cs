using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IContactEntityService
{
    IQueryable<Contact> GetAllQueryable();
    Task<Contact?> GetByIdAsync(int id);
    void Add(Contact entity);
    void AddRange(IEnumerable<Contact> entities);
    void Update(Contact entity);
    void Remove(Contact entity);
}

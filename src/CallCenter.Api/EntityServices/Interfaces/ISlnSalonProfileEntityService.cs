using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnSalonProfileEntityService
{
    IQueryable<SlnSalonProfile> GetAllQueryable();
    Task<SlnSalonProfile?> GetByIdAsync(int id);
    void Add(SlnSalonProfile entity);
    void Update(SlnSalonProfile entity);
    void Remove(SlnSalonProfile entity);
}

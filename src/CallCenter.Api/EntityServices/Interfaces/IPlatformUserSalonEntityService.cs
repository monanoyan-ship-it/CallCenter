using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IPlatformUserSalonEntityService
{
    IQueryable<PlatformUserSalon> GetAllQueryable();
    Task<PlatformUserSalon?> GetByIdAsync(int id);
    void Add(PlatformUserSalon entity);
    void Update(PlatformUserSalon entity);
    void Remove(PlatformUserSalon entity);
}

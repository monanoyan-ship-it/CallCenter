using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IPlatformUserEntityService
{
    IQueryable<PlatformUser> GetAllQueryable();
    Task<PlatformUser?> GetByIdAsync(int id);
    void Add(PlatformUser entity);
    void Update(PlatformUser entity);
    void Remove(PlatformUser entity);
}

using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IPlatformPushTokenEntityService
{
    IQueryable<PlatformPushToken> GetAllQueryable();
    Task<PlatformPushToken?> GetByIdAsync(int id);
    void Add(PlatformPushToken entity);
    void Update(PlatformPushToken entity);
    void Remove(PlatformPushToken entity);
}

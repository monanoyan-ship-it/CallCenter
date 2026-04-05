using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IPlatformEmailTemplateEntityService
{
    Task<PlatformEmailTemplate?> GetByIdAsync(int id);
    Task<PlatformEmailTemplate?> GetByUidAsync(Guid uid);
    Task<PlatformEmailTemplate?> GetByEventKeyAsync(string eventKey, string language = "tr");
    IQueryable<PlatformEmailTemplate> GetAllQueryable();
    void Add(PlatformEmailTemplate entity);
    void Update(PlatformEmailTemplate entity);
    void Remove(PlatformEmailTemplate entity);
}

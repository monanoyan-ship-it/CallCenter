using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IPlatformEmailTemplateEntityService
{
    // Event
    Task<PlatformEmailEvent?> GetEventByIdAsync(int id);
    Task<PlatformEmailEvent?> GetEventByKeyAsync(string eventKey);
    IQueryable<PlatformEmailEvent> GetAllEventsQueryable();
    void AddEvent(PlatformEmailEvent entity);
    void UpdateEvent(PlatformEmailEvent entity);
    void RemoveEvent(PlatformEmailEvent entity);

    // Template (dil bazli)
    Task<PlatformEmailTemplate?> GetTemplateByIdAsync(int id);
    Task<PlatformEmailTemplate?> GetTemplateAsync(string eventKey, string language);
    void AddTemplate(PlatformEmailTemplate entity);
    void UpdateTemplate(PlatformEmailTemplate entity);
    void RemoveTemplate(PlatformEmailTemplate entity);
}

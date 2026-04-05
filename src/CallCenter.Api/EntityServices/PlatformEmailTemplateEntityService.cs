using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class PlatformEmailTemplateEntityService : IPlatformEmailTemplateEntityService
{
    private readonly AppDbContext _db;

    public PlatformEmailTemplateEntityService(AppDbContext db) => _db = db;

    // ─── Event ───

    public Task<PlatformEmailEvent?> GetEventByIdAsync(int id)
        => _db.PlatformEmailEvents.Include(e => e.Templates).FirstOrDefaultAsync(e => e.Id == id);

    public Task<PlatformEmailEvent?> GetEventByKeyAsync(string eventKey)
        => _db.PlatformEmailEvents.Include(e => e.Templates).FirstOrDefaultAsync(e => e.EventKey == eventKey);

    public IQueryable<PlatformEmailEvent> GetAllEventsQueryable()
        => _db.PlatformEmailEvents.Include(e => e.Templates);

    public void AddEvent(PlatformEmailEvent entity) => _db.PlatformEmailEvents.Add(entity);
    public void UpdateEvent(PlatformEmailEvent entity) => _db.PlatformEmailEvents.Update(entity);
    public void RemoveEvent(PlatformEmailEvent entity) => _db.PlatformEmailEvents.Remove(entity);

    // ─── Template ───

    public Task<PlatformEmailTemplate?> GetTemplateByIdAsync(int id)
        => _db.PlatformEmailTemplates.FindAsync(id).AsTask();

    public Task<PlatformEmailTemplate?> GetTemplateAsync(string eventKey, string language)
        => _db.PlatformEmailTemplates
            .Include(t => t.Event)
            .FirstOrDefaultAsync(t => t.Event!.EventKey == eventKey && t.Language == language && t.IsActive && t.Event.IsActive);

    public void AddTemplate(PlatformEmailTemplate entity) => _db.PlatformEmailTemplates.Add(entity);
    public void UpdateTemplate(PlatformEmailTemplate entity) => _db.PlatformEmailTemplates.Update(entity);
    public void RemoveTemplate(PlatformEmailTemplate entity) => _db.PlatformEmailTemplates.Remove(entity);
}

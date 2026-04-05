using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class PlatformEmailTemplateEntityService : IPlatformEmailTemplateEntityService
{
    private readonly AppDbContext _db;

    public PlatformEmailTemplateEntityService(AppDbContext db) => _db = db;

    public Task<PlatformEmailTemplate?> GetByIdAsync(int id)
        => _db.PlatformEmailTemplates.FindAsync(id).AsTask();

    public Task<PlatformEmailTemplate?> GetByUidAsync(Guid uid)
        => _db.PlatformEmailTemplates.FirstOrDefaultAsync(x => x.Uid == uid);

    public Task<PlatformEmailTemplate?> GetByEventKeyAsync(string eventKey, string language = "tr")
        => _db.PlatformEmailTemplates
            .FirstOrDefaultAsync(x => x.EventKey == eventKey && x.Language == language && x.IsActive);

    public IQueryable<PlatformEmailTemplate> GetAllQueryable()
        => _db.PlatformEmailTemplates;

    public void Add(PlatformEmailTemplate entity) => _db.PlatformEmailTemplates.Add(entity);
    public void Update(PlatformEmailTemplate entity) => _db.PlatformEmailTemplates.Update(entity);
    public void Remove(PlatformEmailTemplate entity) => _db.PlatformEmailTemplates.Remove(entity);
}

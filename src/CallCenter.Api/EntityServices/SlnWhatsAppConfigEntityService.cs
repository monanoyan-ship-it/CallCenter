using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnWhatsAppConfigEntityService : ISlnWhatsAppConfigEntityService
{
    private readonly AppDbContext _db;

    public SlnWhatsAppConfigEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnWhatsAppConfig> GetAllQueryable()
        => _db.SlnWhatsAppConfigs.AsQueryable();

    public Task<SlnWhatsAppConfig?> GetByIdAsync(int id)
        => _db.SlnWhatsAppConfigs.FindAsync(id).AsTask();

    public void Add(SlnWhatsAppConfig entity) => _db.SlnWhatsAppConfigs.Add(entity);
    public void Update(SlnWhatsAppConfig entity) => _db.SlnWhatsAppConfigs.Update(entity);
    public void Remove(SlnWhatsAppConfig entity) => _db.SlnWhatsAppConfigs.Remove(entity);
}

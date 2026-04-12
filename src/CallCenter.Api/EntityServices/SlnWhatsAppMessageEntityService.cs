using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnWhatsAppMessageEntityService : ISlnWhatsAppMessageEntityService
{
    private readonly AppDbContext _db;

    public SlnWhatsAppMessageEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnWhatsAppMessage> GetAllQueryable()
        => _db.SlnWhatsAppMessages.AsQueryable();

    public Task<SlnWhatsAppMessage?> GetByIdAsync(int id)
        => _db.SlnWhatsAppMessages.FindAsync(id).AsTask();

    public void Add(SlnWhatsAppMessage entity) => _db.SlnWhatsAppMessages.Add(entity);
    public void Update(SlnWhatsAppMessage entity) => _db.SlnWhatsAppMessages.Update(entity);
    public void Remove(SlnWhatsAppMessage entity) => _db.SlnWhatsAppMessages.Remove(entity);
}

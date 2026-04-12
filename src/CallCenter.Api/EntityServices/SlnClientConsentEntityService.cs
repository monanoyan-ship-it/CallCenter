using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnClientConsentEntityService : ISlnClientConsentEntityService
{
    private readonly AppDbContext _db;

    public SlnClientConsentEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnClientConsent> GetAllQueryable()
        => _db.SlnClientConsents.AsQueryable();

    public Task<SlnClientConsent?> GetByIdAsync(int id)
        => _db.SlnClientConsents.FindAsync(id).AsTask();

    public void Add(SlnClientConsent entity) => _db.SlnClientConsents.Add(entity);
    public void Update(SlnClientConsent entity) => _db.SlnClientConsents.Update(entity);
    public void Remove(SlnClientConsent entity) => _db.SlnClientConsents.Remove(entity);
}

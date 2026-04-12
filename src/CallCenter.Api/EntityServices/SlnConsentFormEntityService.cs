using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnConsentFormEntityService : ISlnConsentFormEntityService
{
    private readonly AppDbContext _db;

    public SlnConsentFormEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnConsentForm> GetAllQueryable()
        => _db.SlnConsentForms.AsQueryable();

    public Task<SlnConsentForm?> GetByIdAsync(int id)
        => _db.SlnConsentForms.FindAsync(id).AsTask();

    public void Add(SlnConsentForm entity) => _db.SlnConsentForms.Add(entity);
    public void Update(SlnConsentForm entity) => _db.SlnConsentForms.Update(entity);
    public void Remove(SlnConsentForm entity) => _db.SlnConsentForms.Remove(entity);
}

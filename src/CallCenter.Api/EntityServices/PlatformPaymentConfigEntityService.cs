using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class PlatformPaymentConfigEntityService : IPlatformPaymentConfigEntityService
{
    private readonly AppDbContext _db;

    public PlatformPaymentConfigEntityService(AppDbContext db) => _db = db;

    public IQueryable<PlatformPaymentConfig> GetAllQueryable()
        => _db.PlatformPaymentConfigs.AsQueryable();

    public Task<PlatformPaymentConfig?> GetByIdAsync(int id)
        => _db.PlatformPaymentConfigs.FindAsync(id).AsTask();

    public void Add(PlatformPaymentConfig entity) => _db.PlatformPaymentConfigs.Add(entity);
    public void Update(PlatformPaymentConfig entity) => _db.PlatformPaymentConfigs.Update(entity);
    public void Remove(PlatformPaymentConfig entity) => _db.PlatformPaymentConfigs.Remove(entity);
}

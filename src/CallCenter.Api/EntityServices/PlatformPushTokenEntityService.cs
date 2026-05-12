using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class PlatformPushTokenEntityService : IPlatformPushTokenEntityService
{
    private readonly AppDbContext _db;

    public PlatformPushTokenEntityService(AppDbContext db) => _db = db;

    public IQueryable<PlatformPushToken> GetAllQueryable()
        => _db.PlatformPushTokens.AsQueryable();

    public Task<PlatformPushToken?> GetByIdAsync(int id)
        => _db.PlatformPushTokens.FindAsync(id).AsTask();

    public void Add(PlatformPushToken entity) => _db.PlatformPushTokens.Add(entity);
    public void Update(PlatformPushToken entity) => _db.PlatformPushTokens.Update(entity);
    public void Remove(PlatformPushToken entity) => _db.PlatformPushTokens.Remove(entity);
}

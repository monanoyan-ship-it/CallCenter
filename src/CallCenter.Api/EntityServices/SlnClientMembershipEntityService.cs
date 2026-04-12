using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnClientMembershipEntityService : ISlnClientMembershipEntityService
{
    private readonly AppDbContext _db;

    public SlnClientMembershipEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnClientMembership> GetAllQueryable()
        => _db.SlnClientMemberships.AsQueryable();

    public Task<SlnClientMembership?> GetByIdAsync(int id)
        => _db.SlnClientMemberships.FindAsync(id).AsTask();

    public void Add(SlnClientMembership entity) => _db.SlnClientMemberships.Add(entity);
    public void Update(SlnClientMembership entity) => _db.SlnClientMemberships.Update(entity);
    public void Remove(SlnClientMembership entity) => _db.SlnClientMemberships.Remove(entity);
}

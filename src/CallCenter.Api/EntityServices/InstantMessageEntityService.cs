using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class InstantMessageEntityService : IInstantMessageEntityService
{
    private readonly AppDbContext _db;

    public InstantMessageEntityService(AppDbContext db) => _db = db;

    public IQueryable<InstantMessage> GetAllQueryable()
        => _db.InstantMessages.AsQueryable();

    public Task<InstantMessage?> GetByIdAsync(int id)
        => _db.InstantMessages.FindAsync(id).AsTask();

    public void Add(InstantMessage entity) => _db.InstantMessages.Add(entity);
}

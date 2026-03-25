using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnClientEntityService : ISlnClientEntityService
{
    private readonly AppDbContext _db;

    public SlnClientEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnClient> GetAllQueryable()
        => _db.SlnClients.AsQueryable();

    public Task<SlnClient?> GetByIdAsync(int id)
        => _db.SlnClients.FindAsync(id).AsTask();

    public void Add(SlnClient entity) => _db.SlnClients.Add(entity);
    public void Update(SlnClient entity) => _db.SlnClients.Update(entity);
    public void Remove(SlnClient entity) => _db.SlnClients.Remove(entity);
}

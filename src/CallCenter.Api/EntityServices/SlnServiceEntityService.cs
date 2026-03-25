using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnServiceEntityService : ISlnServiceEntityService
{
    private readonly AppDbContext _db;

    public SlnServiceEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnService> GetAllQueryable()
        => _db.SlnServices.AsQueryable();

    public Task<SlnService?> GetByIdAsync(int id)
        => _db.SlnServices.FindAsync(id).AsTask();

    public void Add(SlnService entity) => _db.SlnServices.Add(entity);
    public void Update(SlnService entity) => _db.SlnServices.Update(entity);
    public void Remove(SlnService entity) => _db.SlnServices.Remove(entity);
}

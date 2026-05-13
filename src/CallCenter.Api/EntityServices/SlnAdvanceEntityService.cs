using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnAdvanceEntityService : ISlnAdvanceEntityService
{
    private readonly AppDbContext _db;
    public SlnAdvanceEntityService(AppDbContext db) => _db = db;
    public IQueryable<SlnAdvance> GetAllQueryable() => _db.SlnAdvances.AsQueryable();
    public Task<SlnAdvance?> GetByIdAsync(int id) => _db.SlnAdvances.FindAsync(id).AsTask();
    public void Add(SlnAdvance entity) => _db.SlnAdvances.Add(entity);
    public void Update(SlnAdvance entity) => _db.SlnAdvances.Update(entity);
    public void Remove(SlnAdvance entity) => _db.SlnAdvances.Remove(entity);
}

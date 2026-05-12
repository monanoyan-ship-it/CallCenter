using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnServiceComboItemEntityService : ISlnServiceComboItemEntityService
{
    private readonly AppDbContext _db;

    public SlnServiceComboItemEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnServiceComboItem> GetAllQueryable()
        => _db.SlnServiceComboItems.AsQueryable();

    public Task<SlnServiceComboItem?> GetByIdAsync(int id)
        => _db.SlnServiceComboItems.FindAsync(id).AsTask();

    public void Add(SlnServiceComboItem entity) => _db.SlnServiceComboItems.Add(entity);
    public void Remove(SlnServiceComboItem entity) => _db.SlnServiceComboItems.Remove(entity);
    public void RemoveRange(IEnumerable<SlnServiceComboItem> entities) => _db.SlnServiceComboItems.RemoveRange(entities);
}

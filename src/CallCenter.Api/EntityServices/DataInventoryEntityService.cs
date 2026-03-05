using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class DataInventoryEntityService : IDataInventoryEntityService
{
    private readonly AppDbContext _db;

    public DataInventoryEntityService(AppDbContext db) => _db = db;

    public IQueryable<DataInventoryItem> GetAllQueryable() => _db.DataInventoryItems.AsQueryable();
    public Task<DataInventoryItem?> GetByIdAsync(int id) => _db.DataInventoryItems.FindAsync(id).AsTask();
    public void Add(DataInventoryItem entity) => _db.DataInventoryItems.Add(entity);
    public void Update(DataInventoryItem entity) => _db.DataInventoryItems.Update(entity);
    public void Remove(DataInventoryItem entity) => _db.DataInventoryItems.Remove(entity);
}

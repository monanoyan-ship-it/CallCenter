using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnSupplierEntityService : ISlnSupplierEntityService
{
    private readonly AppDbContext _db;

    public SlnSupplierEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnSupplier> GetAllQueryable()
        => _db.SlnSuppliers.AsQueryable();

    public Task<SlnSupplier?> GetByIdAsync(int id)
        => _db.SlnSuppliers.FindAsync(id).AsTask();

    public void Add(SlnSupplier entity) => _db.SlnSuppliers.Add(entity);
    public void Update(SlnSupplier entity) => _db.SlnSuppliers.Update(entity);
    public void Remove(SlnSupplier entity) => _db.SlnSuppliers.Remove(entity);
}

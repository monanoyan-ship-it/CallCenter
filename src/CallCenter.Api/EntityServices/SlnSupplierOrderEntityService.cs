using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnSupplierOrderEntityService : ISlnSupplierOrderEntityService
{
    private readonly AppDbContext _db;

    public SlnSupplierOrderEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnSupplierOrder> GetAllQueryable() => _db.SlnSupplierOrders.AsQueryable();
    public Task<SlnSupplierOrder?> GetByIdAsync(int id) => _db.SlnSupplierOrders.FindAsync(id).AsTask();
    public void Add(SlnSupplierOrder entity) => _db.SlnSupplierOrders.Add(entity);
    public void Update(SlnSupplierOrder entity) => _db.SlnSupplierOrders.Update(entity);
    public void Remove(SlnSupplierOrder entity) => _db.SlnSupplierOrders.Remove(entity);
}

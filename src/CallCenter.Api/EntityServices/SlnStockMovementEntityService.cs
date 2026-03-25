using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnStockMovementEntityService : ISlnStockMovementEntityService
{
    private readonly AppDbContext _db;

    public SlnStockMovementEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnStockMovement> GetAllQueryable()
        => _db.SlnStockMovements.AsQueryable();

    public Task<SlnStockMovement?> GetByIdAsync(int id)
        => _db.SlnStockMovements.FindAsync(id).AsTask();

    public void Add(SlnStockMovement entity) => _db.SlnStockMovements.Add(entity);
    public void Remove(SlnStockMovement entity) => _db.SlnStockMovements.Remove(entity);
}

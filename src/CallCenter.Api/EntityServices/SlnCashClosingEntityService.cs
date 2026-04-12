using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnCashClosingEntityService : ISlnCashClosingEntityService
{
    private readonly AppDbContext _db;

    public SlnCashClosingEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnCashClosing> GetAllQueryable()
        => _db.SlnCashClosings.AsQueryable();

    public Task<SlnCashClosing?> GetByIdAsync(int id)
        => _db.SlnCashClosings.FindAsync(id).AsTask();

    public void Add(SlnCashClosing entity) => _db.SlnCashClosings.Add(entity);
    public void Update(SlnCashClosing entity) => _db.SlnCashClosings.Update(entity);
    public void Remove(SlnCashClosing entity) => _db.SlnCashClosings.Remove(entity);
}

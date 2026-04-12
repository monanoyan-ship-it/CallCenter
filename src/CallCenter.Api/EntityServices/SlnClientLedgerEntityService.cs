using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnClientLedgerEntityService : ISlnClientLedgerEntityService
{
    private readonly AppDbContext _db;

    public SlnClientLedgerEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnClientLedger> GetAllQueryable()
        => _db.SlnClientLedgers.AsQueryable();

    public Task<SlnClientLedger?> GetByIdAsync(int id)
        => _db.SlnClientLedgers.FindAsync(id).AsTask();

    public void Add(SlnClientLedger entity) => _db.SlnClientLedgers.Add(entity);
    public void Update(SlnClientLedger entity) => _db.SlnClientLedgers.Update(entity);
    public void Remove(SlnClientLedger entity) => _db.SlnClientLedgers.Remove(entity);
}

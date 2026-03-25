using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnCashTransactionEntityService : ISlnCashTransactionEntityService
{
    private readonly AppDbContext _db;

    public SlnCashTransactionEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnCashTransaction> GetAllQueryable()
        => _db.SlnCashTransactions.AsQueryable();

    public Task<SlnCashTransaction?> GetByIdAsync(int id)
        => _db.SlnCashTransactions.FindAsync(id).AsTask();

    public void Add(SlnCashTransaction entity) => _db.SlnCashTransactions.Add(entity);
    public void Remove(SlnCashTransaction entity) => _db.SlnCashTransactions.Remove(entity);
}

using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnSupplierTransactionEntityService : ISlnSupplierTransactionEntityService
{
    private readonly AppDbContext _db;

    public SlnSupplierTransactionEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnSupplierTransaction> GetAllQueryable()
        => _db.SlnSupplierTransactions.AsQueryable();

    public Task<SlnSupplierTransaction?> GetByIdAsync(int id)
        => _db.SlnSupplierTransactions.FindAsync(id).AsTask();

    public void Add(SlnSupplierTransaction entity) => _db.SlnSupplierTransactions.Add(entity);
    public void Remove(SlnSupplierTransaction entity) => _db.SlnSupplierTransactions.Remove(entity);
}

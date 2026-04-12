using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnLoyaltyTransactionEntityService : ISlnLoyaltyTransactionEntityService
{
    private readonly AppDbContext _db;

    public SlnLoyaltyTransactionEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnLoyaltyTransaction> GetAllQueryable()
        => _db.SlnLoyaltyTransactions.AsQueryable();

    public Task<SlnLoyaltyTransaction?> GetByIdAsync(int id)
        => _db.SlnLoyaltyTransactions.FindAsync(id).AsTask();

    public void Add(SlnLoyaltyTransaction entity) => _db.SlnLoyaltyTransactions.Add(entity);
    public void Update(SlnLoyaltyTransaction entity) => _db.SlnLoyaltyTransactions.Update(entity);
    public void Remove(SlnLoyaltyTransaction entity) => _db.SlnLoyaltyTransactions.Remove(entity);
}

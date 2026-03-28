using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnGiftCardEntityService : ISlnGiftCardEntityService
{
    private readonly AppDbContext _db;
    public SlnGiftCardEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnGiftCard> GetAllQueryable() => _db.SlnGiftCards.AsQueryable();
    public Task<SlnGiftCard?> GetByIdAsync(int id) => _db.SlnGiftCards.FindAsync(id).AsTask();
    public void Add(SlnGiftCard entity) => _db.SlnGiftCards.Add(entity);
    public void Update(SlnGiftCard entity) => _db.SlnGiftCards.Update(entity);
}

public class SlnGiftCardTransactionEntityService : ISlnGiftCardTransactionEntityService
{
    private readonly AppDbContext _db;
    public SlnGiftCardTransactionEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnGiftCardTransaction> GetAllQueryable() => _db.SlnGiftCardTransactions.AsQueryable();
    public void Add(SlnGiftCardTransaction entity) => _db.SlnGiftCardTransactions.Add(entity);
}

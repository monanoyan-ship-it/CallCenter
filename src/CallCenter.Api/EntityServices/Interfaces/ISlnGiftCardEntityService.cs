using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnGiftCardEntityService
{
    IQueryable<SlnGiftCard> GetAllQueryable();
    Task<SlnGiftCard?> GetByIdAsync(int id);
    void Add(SlnGiftCard entity);
    void Update(SlnGiftCard entity);
}

public interface ISlnGiftCardTransactionEntityService
{
    IQueryable<SlnGiftCardTransaction> GetAllQueryable();
    void Add(SlnGiftCardTransaction entity);
}

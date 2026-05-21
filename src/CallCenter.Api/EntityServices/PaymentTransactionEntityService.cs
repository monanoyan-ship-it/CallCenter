using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class PaymentTransactionEntityService : IPaymentTransactionEntityService
{
    private readonly AppDbContext _db;

    public PaymentTransactionEntityService(AppDbContext db) => _db = db;

    public IQueryable<PaymentTransaction> GetAllQueryable()
        => _db.PaymentTransactions.AsQueryable();
}

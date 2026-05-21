using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IPaymentTransactionEntityService
{
    IQueryable<PaymentTransaction> GetAllQueryable();
}

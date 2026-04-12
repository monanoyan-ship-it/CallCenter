using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnInvoiceRefundEntityService
{
    IQueryable<SlnInvoiceRefund> GetAllQueryable();
    Task<SlnInvoiceRefund?> GetByIdAsync(int id);
    void Add(SlnInvoiceRefund entity);
    void Update(SlnInvoiceRefund entity);
    void Remove(SlnInvoiceRefund entity);
}

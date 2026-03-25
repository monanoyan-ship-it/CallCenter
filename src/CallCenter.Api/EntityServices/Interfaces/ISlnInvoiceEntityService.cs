using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnInvoiceEntityService
{
    IQueryable<SlnInvoice> GetAllQueryable();
    Task<SlnInvoice?> GetByIdAsync(int id);
    void Add(SlnInvoice entity);
    void Update(SlnInvoice entity);
    void Remove(SlnInvoice entity);
}

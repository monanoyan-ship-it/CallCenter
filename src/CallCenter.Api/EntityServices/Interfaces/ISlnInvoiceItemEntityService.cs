using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnInvoiceItemEntityService
{
    IQueryable<SlnInvoiceItem> GetAllQueryable();
    Task<SlnInvoiceItem?> GetByIdAsync(int id);
    void Add(SlnInvoiceItem entity);
    void AddRange(IEnumerable<SlnInvoiceItem> entities);
    void Remove(SlnInvoiceItem entity);
    void RemoveRange(IEnumerable<SlnInvoiceItem> entities);
}

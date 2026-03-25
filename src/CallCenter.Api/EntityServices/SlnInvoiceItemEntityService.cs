using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnInvoiceItemEntityService : ISlnInvoiceItemEntityService
{
    private readonly AppDbContext _db;

    public SlnInvoiceItemEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnInvoiceItem> GetAllQueryable()
        => _db.SlnInvoiceItems.AsQueryable();

    public Task<SlnInvoiceItem?> GetByIdAsync(int id)
        => _db.SlnInvoiceItems.FindAsync(id).AsTask();

    public void Add(SlnInvoiceItem entity) => _db.SlnInvoiceItems.Add(entity);
    public void AddRange(IEnumerable<SlnInvoiceItem> entities) => _db.SlnInvoiceItems.AddRange(entities);
    public void Remove(SlnInvoiceItem entity) => _db.SlnInvoiceItems.Remove(entity);
    public void RemoveRange(IEnumerable<SlnInvoiceItem> entities) => _db.SlnInvoiceItems.RemoveRange(entities);
}

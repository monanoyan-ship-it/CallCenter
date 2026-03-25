using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnInvoiceEntityService : ISlnInvoiceEntityService
{
    private readonly AppDbContext _db;

    public SlnInvoiceEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnInvoice> GetAllQueryable()
        => _db.SlnInvoices.AsQueryable();

    public Task<SlnInvoice?> GetByIdAsync(int id)
        => _db.SlnInvoices.FindAsync(id).AsTask();

    public void Add(SlnInvoice entity) => _db.SlnInvoices.Add(entity);
    public void Update(SlnInvoice entity) => _db.SlnInvoices.Update(entity);
    public void Remove(SlnInvoice entity) => _db.SlnInvoices.Remove(entity);
}

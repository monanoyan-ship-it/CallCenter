using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnInvoiceRefundEntityService : ISlnInvoiceRefundEntityService
{
    private readonly AppDbContext _db;

    public SlnInvoiceRefundEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnInvoiceRefund> GetAllQueryable()
        => _db.SlnInvoiceRefunds.AsQueryable();

    public Task<SlnInvoiceRefund?> GetByIdAsync(int id)
        => _db.SlnInvoiceRefunds.FindAsync(id).AsTask();

    public void Add(SlnInvoiceRefund entity) => _db.SlnInvoiceRefunds.Add(entity);
    public void Update(SlnInvoiceRefund entity) => _db.SlnInvoiceRefunds.Update(entity);
    public void Remove(SlnInvoiceRefund entity) => _db.SlnInvoiceRefunds.Remove(entity);
}

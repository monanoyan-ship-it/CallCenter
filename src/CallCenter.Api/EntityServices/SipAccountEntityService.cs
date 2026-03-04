using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class SipAccountEntityService : ISipAccountEntityService
{
    private readonly AppDbContext _db;

    public SipAccountEntityService(AppDbContext db) => _db = db;

    public IQueryable<SipAccount> GetAllQueryable()
        => _db.SipAccounts.AsQueryable();

    public Task<SipAccount?> GetByIdAsync(int id)
        => _db.SipAccounts.FindAsync(id).AsTask();

    public Task<SipAccount?> GetByIdWithCustomerAsync(int id)
        => _db.SipAccounts.Include(s => s.Customer).FirstOrDefaultAsync(s => s.Id == id);

    public Task<SipAccount?> GetDefaultByCustomerAsync(int customerId)
        => _db.SipAccounts.FirstOrDefaultAsync(s => s.CustomerId == customerId && s.IsDefault && s.IsActive);

    public Task<SipAccount?> GetFirstActiveByCustomerAsync(int customerId)
        => _db.SipAccounts.FirstOrDefaultAsync(s => s.CustomerId == customerId && s.IsActive);

    public Task<SipAccount?> GetExistingDefaultAsync(int customerId, int? excludeId = null)
    {
        var query = _db.SipAccounts.Where(s => s.CustomerId == customerId && s.IsDefault && s.IsActive);
        if (excludeId.HasValue)
            query = query.Where(s => s.Id != excludeId.Value);
        return query.FirstOrDefaultAsync();
    }

    public void Add(SipAccount entity) => _db.SipAccounts.Add(entity);
    public void Update(SipAccount entity) => _db.SipAccounts.Update(entity);
}

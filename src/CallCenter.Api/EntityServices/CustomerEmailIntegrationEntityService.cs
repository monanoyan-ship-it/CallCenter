using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CustomerEmailIntegrationEntityService : ICustomerEmailIntegrationEntityService
{
    private readonly AppDbContext _db;

    public CustomerEmailIntegrationEntityService(AppDbContext db) => _db = db;

    public Task<CustomerEmailIntegration?> GetByIdAsync(int id)
        => _db.CustomerEmailIntegrations.FindAsync(id).AsTask();

    public Task<CustomerEmailIntegration?> GetByUidAsync(Guid uid)
        => _db.CustomerEmailIntegrations.FirstOrDefaultAsync(x => x.Uid == uid);

    public IQueryable<CustomerEmailIntegration> GetByCustomerQueryable(int customerId)
        => _db.CustomerEmailIntegrations.Where(x => x.CustomerId == customerId);

    public Task<CustomerEmailIntegration?> GetDefaultAsync(int customerId)
        => _db.CustomerEmailIntegrations
            .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.IsActive && x.IsDefault);

    public void Add(CustomerEmailIntegration entity) => _db.CustomerEmailIntegrations.Add(entity);
    public void Update(CustomerEmailIntegration entity) => _db.CustomerEmailIntegrations.Update(entity);
    public void Remove(CustomerEmailIntegration entity) => _db.CustomerEmailIntegrations.Remove(entity);
}

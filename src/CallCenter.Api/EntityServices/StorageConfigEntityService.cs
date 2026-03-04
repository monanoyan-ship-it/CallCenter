using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class StorageConfigEntityService : IStorageConfigEntityService
{
    private readonly AppDbContext _db;

    public StorageConfigEntityService(AppDbContext db) => _db = db;

    public IQueryable<CustomerStorageConfig> GetAllQueryable()
        => _db.CustomerStorageConfigs.AsQueryable();

    public Task<CustomerStorageConfig?> GetByIdAsync(int id)
        => _db.CustomerStorageConfigs.FindAsync(id).AsTask();

    public Task<CustomerStorageConfig?> GetDefaultByCustomerAsync(int customerId)
        => _db.CustomerStorageConfigs
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.IsDefault);

    public void Add(CustomerStorageConfig entity) => _db.CustomerStorageConfigs.Add(entity);
    public void Remove(CustomerStorageConfig entity) => _db.CustomerStorageConfigs.Remove(entity);
}

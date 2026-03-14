using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class IntegrationApiKeyEntityService : IIntegrationApiKeyEntityService
{
    private readonly AppDbContext _db;

    public IntegrationApiKeyEntityService(AppDbContext db) => _db = db;

    public Task<IntegrationApiKey?> GetByIdAsync(int id)
        => _db.IntegrationApiKeys.FindAsync(id).AsTask();

    public Task<IntegrationApiKey?> GetByUidAsync(Guid uid)
        => _db.IntegrationApiKeys.FirstOrDefaultAsync(x => x.Uid == uid);

    public async Task<IntegrationApiKey?> GetByHashAsync(string apiKeyHash)
    {
        return await _db.IntegrationApiKeys
            .Include(x => x.Customer)
            .Include(x => x.IntegrationConnection)
            .FirstOrDefaultAsync(x => x.ApiKeyHash == apiKeyHash && x.IsActive);
    }

    public IQueryable<IntegrationApiKey> GetByConnectionQueryable(int connectionId)
        => _db.IntegrationApiKeys.Where(x => x.IntegrationConnectionId == connectionId);

    public IQueryable<IntegrationApiKey> GetByCustomerQueryable(int customerId)
        => _db.IntegrationApiKeys.Where(x => x.CustomerId == customerId);

    public void Add(IntegrationApiKey entity) => _db.IntegrationApiKeys.Add(entity);
    public void Update(IntegrationApiKey entity) => _db.IntegrationApiKeys.Update(entity);
    public void Remove(IntegrationApiKey entity) => _db.IntegrationApiKeys.Remove(entity);
}

using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class IntegrationConnectionEntityService : IIntegrationConnectionEntityService
{
    private readonly AppDbContext _db;

    public IntegrationConnectionEntityService(AppDbContext db) => _db = db;

    public Task<IntegrationConnection?> GetByIdAsync(int id)
        => _db.IntegrationConnections.FindAsync(id).AsTask();

    public Task<IntegrationConnection?> GetByUidAsync(Guid uid)
        => _db.IntegrationConnections.FirstOrDefaultAsync(x => x.Uid == uid);

    public Task<IntegrationConnection?> GetByIdWithDetailsAsync(int id)
        => _db.IntegrationConnections
            .Include(x => x.WebhookSubscriptions)
            .Include(x => x.ApiKeys)
            .FirstOrDefaultAsync(x => x.Id == id);

    public IQueryable<IntegrationConnection> GetAllQueryable()
        => _db.IntegrationConnections.AsQueryable();

    public IQueryable<IntegrationConnection> GetByCustomerQueryable(int customerId)
        => _db.IntegrationConnections.Where(x => x.CustomerId == customerId);

    public void Add(IntegrationConnection entity) => _db.IntegrationConnections.Add(entity);
    public void Update(IntegrationConnection entity) => _db.IntegrationConnections.Update(entity);
    public void Remove(IntegrationConnection entity) => _db.IntegrationConnections.Remove(entity);
}

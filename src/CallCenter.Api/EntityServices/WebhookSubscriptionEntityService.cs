using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class WebhookSubscriptionEntityService : IWebhookSubscriptionEntityService
{
    private readonly AppDbContext _db;

    public WebhookSubscriptionEntityService(AppDbContext db) => _db = db;

    public Task<WebhookSubscription?> GetByIdAsync(int id)
        => _db.WebhookSubscriptions.FindAsync(id).AsTask();

    public Task<WebhookSubscription?> GetByUidAsync(Guid uid)
        => _db.WebhookSubscriptions.FirstOrDefaultAsync(x => x.Uid == uid);

    public IQueryable<WebhookSubscription> GetAllQueryable()
        => _db.WebhookSubscriptions.AsQueryable();

    public IQueryable<WebhookSubscription> GetByCustomerQueryable(int customerId)
        => _db.WebhookSubscriptions.Where(x => x.CustomerId == customerId);

    public async Task<List<WebhookSubscription>> GetActiveByCustomerAndEventAsync(int customerId, string eventType)
    {
        // Aktif abonelikleri getir: EventFilter null (tum eventler) veya eventType iceriyor
        return await _db.WebhookSubscriptions
            .Where(x => x.CustomerId == customerId && x.IsActive)
            .Where(x => x.EventFilter == null || x.EventFilter.Contains(eventType))
            .ToListAsync();
    }

    public void Add(WebhookSubscription entity) => _db.WebhookSubscriptions.Add(entity);
    public void Update(WebhookSubscription entity) => _db.WebhookSubscriptions.Update(entity);
    public void Remove(WebhookSubscription entity) => _db.WebhookSubscriptions.Remove(entity);
}

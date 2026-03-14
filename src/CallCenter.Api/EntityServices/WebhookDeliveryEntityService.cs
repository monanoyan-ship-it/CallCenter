using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class WebhookDeliveryEntityService : IWebhookDeliveryEntityService
{
    private readonly AppDbContext _db;

    public WebhookDeliveryEntityService(AppDbContext db) => _db = db;

    public Task<WebhookDelivery?> GetByIdAsync(long id)
        => _db.WebhookDeliveries.FindAsync(id).AsTask();

    public IQueryable<WebhookDelivery> GetBySubscriptionQueryable(int subscriptionId)
        => _db.WebhookDeliveries
            .Where(x => x.WebhookSubscriptionId == subscriptionId)
            .OrderByDescending(x => x.CreatedAt);

    public async Task<List<WebhookDelivery>> GetPendingRetriesAsync(int batchSize = 50)
    {
        var now = DateTime.UtcNow;
        return await _db.WebhookDeliveries
            .Where(x => x.NextRetryAt != null && x.NextRetryAt <= now && x.DeliveredAt == null)
            .OrderBy(x => x.NextRetryAt)
            .Take(batchSize)
            .Include(x => x.WebhookSubscription)
            .ToListAsync();
    }

    public async Task<bool> IsEventDeliveredAsync(int subscriptionId, string eventId)
    {
        return await _db.WebhookDeliveries
            .AnyAsync(x => x.WebhookSubscriptionId == subscriptionId
                        && x.EventId == eventId
                        && x.DeliveredAt != null);
    }

    public void Add(WebhookDelivery entity) => _db.WebhookDeliveries.Add(entity);
    public void Update(WebhookDelivery entity) => _db.WebhookDeliveries.Update(entity);
}

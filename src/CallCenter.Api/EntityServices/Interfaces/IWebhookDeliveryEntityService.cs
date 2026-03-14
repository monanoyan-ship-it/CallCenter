using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IWebhookDeliveryEntityService
{
    Task<WebhookDelivery?> GetByIdAsync(long id);
    IQueryable<WebhookDelivery> GetBySubscriptionQueryable(int subscriptionId);
    /// <summary>Retry bekleyen delivery'leri getirir (dispatch service icin)</summary>
    Task<List<WebhookDelivery>> GetPendingRetriesAsync(int batchSize = 50);
    /// <summary>Ayni event_id ile daha once basarili gonderim yapilmis mi</summary>
    Task<bool> IsEventDeliveredAsync(int subscriptionId, string eventId);
    void Add(WebhookDelivery entity);
    void Update(WebhookDelivery entity);
}

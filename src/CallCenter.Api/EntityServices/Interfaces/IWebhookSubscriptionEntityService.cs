using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IWebhookSubscriptionEntityService
{
    Task<WebhookSubscription?> GetByIdAsync(int id);
    Task<WebhookSubscription?> GetByUidAsync(Guid uid);
    IQueryable<WebhookSubscription> GetAllQueryable();
    IQueryable<WebhookSubscription> GetByCustomerQueryable(int customerId);
    /// <summary>Belirli bir event tipini dinleyen aktif abonelikleri getirir (dispatch icin)</summary>
    Task<List<WebhookSubscription>> GetActiveByCustomerAndEventAsync(int customerId, string eventType);
    void Add(WebhookSubscription entity);
    void Update(WebhookSubscription entity);
    void Remove(WebhookSubscription entity);
}

using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

/// <summary>
/// Entegrasyon baglantilari, webhook abonelikleri ve API key yonetimi.
/// </summary>
public interface IIntegrationFactory
{
    // ─── Connection CRUD ───
    Task<List<IntegrationConnectionListDto>> GetConnectionsAsync(int customerId);
    Task<IntegrationConnectionDetailDto?> GetConnectionDetailAsync(int customerId, Guid connectionUid);
    Task<(int Id, string? Error)> CreateConnectionAsync(int customerId, CreateIntegrationConnectionRequest dto);
    Task<(bool Success, string? Error)> UpdateConnectionAsync(int customerId, Guid connectionUid, UpdateIntegrationConnectionRequest dto);
    Task<(bool Success, string? Error)> DeleteConnectionAsync(int customerId, Guid connectionUid);
    Task<ConnectionTestResult> TestConnectionAsync(int customerId, Guid connectionUid);

    // ─── OAuth2 ───
    Task<OAuthInitiateResponse?> InitiateOAuthAsync(int customerId, int platformTypeId, string callbackUrl);
    Task<(bool Success, string? Error)> HandleOAuthCallbackAsync(int customerId, OAuthCallbackRequest request);

    // ─── Webhook Subscription CRUD ───
    Task<List<WebhookSubscriptionDto>> GetWebhooksAsync(int customerId);
    Task<(int Id, string? Error)> CreateWebhookAsync(int customerId, CreateWebhookSubscriptionRequest dto);
    Task<(bool Success, string? Error)> UpdateWebhookAsync(int customerId, Guid webhookUid, UpdateWebhookSubscriptionRequest dto);
    Task<(bool Success, string? Error)> DeleteWebhookAsync(int customerId, Guid webhookUid);
    Task<(bool Success, string? Error)> SendTestWebhookAsync(int customerId, Guid webhookUid);

    // ─── Webhook Delivery Log ───
    Task<PagedResult<WebhookDeliveryDto>> GetDeliveriesAsync(int customerId, Guid webhookUid, int page = 1, int pageSize = 50);

    // ─── API Key CRUD ───
    Task<List<IntegrationApiKeyDto>> GetApiKeysAsync(int customerId, int? connectionId = null);
    Task<CreateApiKeyResponse?> CreateApiKeyAsync(int customerId, CreateApiKeyRequest dto);
    Task<(bool Success, string? Error)> RevokeApiKeyAsync(int customerId, Guid apiKeyUid);
}

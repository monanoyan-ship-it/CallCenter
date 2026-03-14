using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

/// <summary>
/// Entegrasyon baglantilari, webhook ve API key yonetimi.
/// </summary>
public class IntegrationFactory : IIntegrationFactory
{
    private readonly IIntegrationConnectionEntityService _connectionEs;
    private readonly IWebhookSubscriptionEntityService _webhookEs;
    private readonly IWebhookDeliveryEntityService _deliveryEs;
    private readonly IIntegrationApiKeyEntityService _apiKeyEs;
    private readonly IUnitOfWork _uow;
    private readonly AesEncryptionService _aes;

    public IntegrationFactory(
        IIntegrationConnectionEntityService connectionEs,
        IWebhookSubscriptionEntityService webhookEs,
        IWebhookDeliveryEntityService deliveryEs,
        IIntegrationApiKeyEntityService apiKeyEs,
        IUnitOfWork uow,
        AesEncryptionService aes)
    {
        _connectionEs = connectionEs;
        _webhookEs = webhookEs;
        _deliveryEs = deliveryEs;
        _apiKeyEs = apiKeyEs;
        _uow = uow;
        _aes = aes;
    }

    // ═══════════════════════════════════════════════════════════
    // Connection CRUD
    // ═══════════════════════════════════════════════════════════

    public async Task<List<IntegrationConnectionListDto>> GetConnectionsAsync(int customerId)
    {
        var connections = await _connectionEs.GetByCustomerQueryable(customerId)
            .Include(x => x.WebhookSubscriptions)
            .Include(x => x.ApiKeys)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return connections.Select(MapToListDto).ToList();
    }

    public async Task<IntegrationConnectionDetailDto?> GetConnectionDetailAsync(int customerId, Guid connectionUid)
    {
        var conn = await _connectionEs.GetByCustomerQueryable(customerId)
            .Include(x => x.WebhookSubscriptions)
            .Include(x => x.ApiKeys)
            .FirstOrDefaultAsync(x => x.Uid == connectionUid);

        if (conn == null) return null;

        var dto = new IntegrationConnectionDetailDto
        {
            Id = conn.Id,
            Uid = conn.Uid,
            Name = conn.Name,
            PlatformTypeId = conn.PlatformTypeId,
            IsActive = conn.IsActive,
            IsConnected = conn.IsConnected,
            AutoLogCalls = conn.AutoLogCalls,
            ContactSyncDirectionId = conn.ContactSyncDirectionId,
            LastSyncAt = conn.LastSyncAt,
            LastError = conn.LastError,
            LastTestedAt = conn.LastTestedAt,
            LastTestSuccess = conn.LastTestSuccess,
            CreatedAt = conn.CreatedAt,
            WebhookCount = conn.WebhookSubscriptions.Count,
            ApiKeyCount = conn.ApiKeys.Count,
            Webhooks = conn.WebhookSubscriptions.Select(MapToWebhookDto).ToList(),
            ApiKeys = conn.ApiKeys.Select(MapToApiKeyDto).ToList()
        };

        var platform = IntegrationPlatforms.GetById(conn.PlatformTypeId);
        if (platform != null)
        {
            dto.PlatformName = platform.SystemName;
            dto.PlatformIcon = platform.Icon ?? "";
            dto.PlatformCss = platform.CssClass ?? "";
        }

        return dto;
    }

    public async Task<(int Id, string? Error)> CreateConnectionAsync(int customerId, CreateIntegrationConnectionRequest dto)
    {
        if (IntegrationPlatforms.GetById(dto.PlatformTypeId) == null)
            return (0, "Gecersiz platform tipi.");

        var conn = new IntegrationConnection
        {
            CustomerId = customerId,
            PlatformTypeId = dto.PlatformTypeId,
            Name = dto.Name,
            AutoLogCalls = dto.AutoLogCalls,
            ContactSyncDirectionId = dto.ContactSyncDirectionId,
            IsActive = true,
            IsConnected = false
        };

        // Credential varsa sifrele
        if (dto.Credentials != null && dto.Credentials.Count > 0)
        {
            var json = JsonSerializer.Serialize(dto.Credentials);
            conn.EncryptedCredentials = _aes.Encrypt(json);
        }

        _connectionEs.Add(conn);
        await _uow.SaveChangesAsync();
        return (conn.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateConnectionAsync(int customerId, Guid connectionUid, UpdateIntegrationConnectionRequest dto)
    {
        var conn = await _connectionEs.GetByCustomerQueryable(customerId)
            .FirstOrDefaultAsync(x => x.Uid == connectionUid);
        if (conn == null) return (false, "Baglanti bulunamadi.");

        if (dto.Name != null) conn.Name = dto.Name;
        if (dto.IsActive.HasValue) conn.IsActive = dto.IsActive.Value;
        if (dto.AutoLogCalls.HasValue) conn.AutoLogCalls = dto.AutoLogCalls.Value;
        if (dto.ContactSyncDirectionId.HasValue) conn.ContactSyncDirectionId = dto.ContactSyncDirectionId.Value;

        if (dto.Credentials != null && dto.Credentials.Count > 0)
        {
            var json = JsonSerializer.Serialize(dto.Credentials);
            conn.EncryptedCredentials = _aes.Encrypt(json);
        }

        conn.UpdatedAt = DateTime.UtcNow;
        _connectionEs.Update(conn);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteConnectionAsync(int customerId, Guid connectionUid)
    {
        var conn = await _connectionEs.GetByCustomerQueryable(customerId)
            .FirstOrDefaultAsync(x => x.Uid == connectionUid);
        if (conn == null) return (false, "Baglanti bulunamadi.");

        _connectionEs.Remove(conn);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(int customerId, Guid connectionUid)
    {
        var conn = await _connectionEs.GetByCustomerQueryable(customerId)
            .FirstOrDefaultAsync(x => x.Uid == connectionUid);
        if (conn == null)
            return new ConnectionTestResult { Success = false, Error = "Baglanti bulunamadi." };

        // Credential'lari coz
        Dictionary<string, string>? creds = null;
        if (!string.IsNullOrEmpty(conn.EncryptedCredentials))
        {
            try
            {
                var decrypted = _aes.Decrypt(conn.EncryptedCredentials);
                creds = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted);
            }
            catch
            {
                return new ConnectionTestResult { Success = false, Error = "Credential bilgileri cozulemedi." };
            }
        }

        // Platform bazli test (connector adapter'lar Adim 5-6'da eklenecek)
        var result = conn.PlatformTypeId switch
        {
            IntegrationPlatforms.Ids.Salesforce => await TestSalesforceAsync(creds),
            IntegrationPlatforms.Ids.HubSpot => await TestHubSpotAsync(creds),
            IntegrationPlatforms.Ids.Zendesk => await TestZendeskAsync(creds),
            _ => new ConnectionTestResult { Success = true, PlatformInfo = "Genel webhook — baglanti testi gerekmez." }
        };

        // Sonucu kaydet
        conn.LastTestedAt = DateTime.UtcNow;
        conn.LastTestSuccess = result.Success;
        conn.IsConnected = result.Success;
        if (!result.Success) conn.LastError = result.Error;
        _connectionEs.Update(conn);
        await _uow.SaveChangesAsync();

        return result;
    }

    // ═══════════════════════════════════════════════════════════
    // OAuth2
    // ═══════════════════════════════════════════════════════════

    public Task<OAuthInitiateResponse?> InitiateOAuthAsync(int customerId, int platformTypeId, string callbackUrl)
    {
        // Salesforce / HubSpot OAuth2 URL'i uretir (Adim 5-6'da detaylandirilacak)
        var state = $"{customerId}:{platformTypeId}:{Guid.NewGuid():N}";

        string? authUrl = platformTypeId switch
        {
            IntegrationPlatforms.Ids.Salesforce =>
                $"https://login.salesforce.com/services/oauth2/authorize?response_type=code&client_id={{CLIENT_ID}}&redirect_uri={Uri.EscapeDataString(callbackUrl)}&state={state}&scope=api+refresh_token+offline_access",
            IntegrationPlatforms.Ids.HubSpot =>
                $"https://app.hubspot.com/oauth/authorize?client_id={{CLIENT_ID}}&redirect_uri={Uri.EscapeDataString(callbackUrl)}&scope=crm.objects.contacts.read+crm.objects.contacts.write&state={state}",
            _ => null
        };

        if (authUrl == null)
            return Task.FromResult<OAuthInitiateResponse?>(null);

        return Task.FromResult<OAuthInitiateResponse?>(new OAuthInitiateResponse
        {
            AuthorizationUrl = authUrl,
            State = state
        });
    }

    public Task<(bool Success, string? Error)> HandleOAuthCallbackAsync(int customerId, OAuthCallbackRequest request)
    {
        // Token exchange (Adim 5-6'da detaylandirilacak)
        return Task.FromResult((false, (string?)"OAuth2 callback henuz implemente edilmedi. Platform connector eklenecek."));
    }

    // ═══════════════════════════════════════════════════════════
    // Webhook Subscription CRUD
    // ═══════════════════════════════════════════════════════════

    public async Task<List<WebhookSubscriptionDto>> GetWebhooksAsync(int customerId)
    {
        var webhooks = await _webhookEs.GetByCustomerQueryable(customerId)
            .Include(x => x.IntegrationConnection)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return webhooks.Select(MapToWebhookDto).ToList();
    }

    public async Task<(int Id, string? Error)> CreateWebhookAsync(int customerId, CreateWebhookSubscriptionRequest dto)
    {
        // Connection varsa ayni musteriye mi ait kontrol et
        if (dto.IntegrationConnectionId.HasValue)
        {
            var conn = await _connectionEs.GetByIdAsync(dto.IntegrationConnectionId.Value);
            if (conn == null || conn.CustomerId != customerId)
                return (0, "Entegrasyon baglantisi bulunamadi.");
        }

        var webhook = new WebhookSubscription
        {
            CustomerId = customerId,
            IntegrationConnectionId = dto.IntegrationConnectionId,
            Name = dto.Name,
            TargetUrl = dto.TargetUrl,
            Secret = GenerateWebhookSecret(),
            EventFilter = dto.EventFilter != null ? JsonSerializer.Serialize(dto.EventFilter) : null,
            IsActive = true
        };

        _webhookEs.Add(webhook);
        await _uow.SaveChangesAsync();
        return (webhook.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateWebhookAsync(int customerId, Guid webhookUid, UpdateWebhookSubscriptionRequest dto)
    {
        var webhook = await _webhookEs.GetByCustomerQueryable(customerId)
            .FirstOrDefaultAsync(x => x.Uid == webhookUid);
        if (webhook == null) return (false, "Webhook bulunamadi.");

        if (dto.Name != null) webhook.Name = dto.Name;
        if (dto.TargetUrl != null) webhook.TargetUrl = dto.TargetUrl;
        if (dto.IsActive.HasValue) webhook.IsActive = dto.IsActive.Value;
        if (dto.EventFilter != null) webhook.EventFilter = JsonSerializer.Serialize(dto.EventFilter);

        webhook.UpdatedAt = DateTime.UtcNow;
        _webhookEs.Update(webhook);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteWebhookAsync(int customerId, Guid webhookUid)
    {
        var webhook = await _webhookEs.GetByCustomerQueryable(customerId)
            .FirstOrDefaultAsync(x => x.Uid == webhookUid);
        if (webhook == null) return (false, "Webhook bulunamadi.");

        _webhookEs.Remove(webhook);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SendTestWebhookAsync(int customerId, Guid webhookUid)
    {
        var webhook = await _webhookEs.GetByCustomerQueryable(customerId)
            .FirstOrDefaultAsync(x => x.Uid == webhookUid);
        if (webhook == null) return (false, "Webhook bulunamadi.");

        // Test event olustur — WebhookEventPublisher'a gonderilir
        // Adim 3'te (Webhook Engine) detaylandirilacak
        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════
    // Webhook Delivery Log
    // ═══════════════════════════════════════════════════════════

    public async Task<PagedResult<WebhookDeliveryDto>> GetDeliveriesAsync(int customerId, Guid webhookUid, int page = 1, int pageSize = 50)
    {
        var webhook = await _webhookEs.GetByCustomerQueryable(customerId)
            .FirstOrDefaultAsync(x => x.Uid == webhookUid);
        if (webhook == null)
            return new PagedResult<WebhookDeliveryDto> { Items = new(), TotalCount = 0, Page = page, PageSize = pageSize };

        var query = _deliveryEs.GetBySubscriptionQueryable(webhook.Id);
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<WebhookDeliveryDto>
        {
            Items = items.Select(d => new WebhookDeliveryDto
            {
                Id = d.Id,
                EventType = d.EventType,
                EventId = d.EventId,
                StatusCode = d.StatusCode,
                ErrorMessage = d.ErrorMessage,
                Attempt = d.Attempt,
                NextRetryAt = d.NextRetryAt,
                DeliveredAt = d.DeliveredAt,
                CreatedAt = d.CreatedAt
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // ═══════════════════════════════════════════════════════════
    // API Key CRUD
    // ═══════════════════════════════════════════════════════════

    public async Task<List<IntegrationApiKeyDto>> GetApiKeysAsync(int customerId, int? connectionId = null)
    {
        var query = _apiKeyEs.GetByCustomerQueryable(customerId);
        if (connectionId.HasValue)
            query = query.Where(x => x.IntegrationConnectionId == connectionId.Value);

        var keys = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return keys.Select(MapToApiKeyDto).ToList();
    }

    public async Task<CreateApiKeyResponse?> CreateApiKeyAsync(int customerId, CreateApiKeyRequest dto)
    {
        // Connection varsa ayni musteriye mi ait kontrol
        var conn = await _connectionEs.GetByIdAsync(dto.IntegrationConnectionId);
        if (conn == null || conn.CustomerId != customerId) return null;

        // API key uret (prefix: clk_ + 40 random char)
        var rawKey = $"clk_{GenerateRandomKey(40)}";
        var hash = HashApiKey(rawKey);

        var apiKey = new IntegrationApiKey
        {
            CustomerId = customerId,
            IntegrationConnectionId = dto.IntegrationConnectionId,
            Name = dto.Name,
            ApiKeyHash = hash,
            ApiKeyPrefix = rawKey[..12], // "clk_XXXXXXXX"
            Scopes = JsonSerializer.Serialize(dto.Scopes),
            ExpiresAt = dto.ExpiresAt,
            IsActive = true
        };

        _apiKeyEs.Add(apiKey);
        await _uow.SaveChangesAsync();

        return new CreateApiKeyResponse
        {
            Id = apiKey.Id,
            Uid = apiKey.Uid,
            Name = apiKey.Name,
            ApiKey = rawKey, // Sadece bu sefer gosterilir!
            ApiKeyPrefix = apiKey.ApiKeyPrefix,
            Scopes = dto.Scopes
        };
    }

    public async Task<(bool Success, string? Error)> RevokeApiKeyAsync(int customerId, Guid apiKeyUid)
    {
        var key = await _apiKeyEs.GetByCustomerQueryable(customerId)
            .FirstOrDefaultAsync(x => x.Uid == apiKeyUid);
        if (key == null) return (false, "API key bulunamadi.");

        key.IsActive = false;
        _apiKeyEs.Update(key);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════
    // Platform-specific test metotlari (skeleton — Adim 5-6'da detaylandirilacak)
    // ═══════════════════════════════════════════════════════════

    private async Task<ConnectionTestResult> TestSalesforceAsync(Dictionary<string, string>? creds)
    {
        if (creds == null || !creds.ContainsKey("InstanceUrl") || !creds.ContainsKey("AccessToken"))
            return new ConnectionTestResult { Success = false, Error = "Salesforce credential bilgileri eksik (InstanceUrl, AccessToken gerekli)." };

        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", creds["AccessToken"]);
            var response = await http.GetAsync($"{creds["InstanceUrl"]}/services/data/v62.0/");
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return new ConnectionTestResult { Success = true, PlatformInfo = $"Salesforce API v62.0 baglantisi basarili." };
            }
            return new ConnectionTestResult { Success = false, Error = $"Salesforce API hatasi: {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult { Success = false, Error = $"Baglanti hatasi: {ex.Message}" };
        }
    }

    private async Task<ConnectionTestResult> TestHubSpotAsync(Dictionary<string, string>? creds)
    {
        if (creds == null || !creds.ContainsKey("AccessToken"))
            return new ConnectionTestResult { Success = false, Error = "HubSpot credential bilgileri eksik (AccessToken gerekli)." };

        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", creds["AccessToken"]);
            var response = await http.GetAsync("https://api.hubapi.com/crm/v3/objects/contacts?limit=1");
            if (response.IsSuccessStatusCode)
                return new ConnectionTestResult { Success = true, PlatformInfo = "HubSpot CRM baglantisi basarili." };
            return new ConnectionTestResult { Success = false, Error = $"HubSpot API hatasi: {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult { Success = false, Error = $"Baglanti hatasi: {ex.Message}" };
        }
    }

    private async Task<ConnectionTestResult> TestZendeskAsync(Dictionary<string, string>? creds)
    {
        if (creds == null || !creds.ContainsKey("Subdomain") || !creds.ContainsKey("Email") || !creds.ContainsKey("ApiToken"))
            return new ConnectionTestResult { Success = false, Error = "Zendesk credential bilgileri eksik (Subdomain, Email, ApiToken gerekli)." };

        try
        {
            using var http = new HttpClient();
            var authBytes = Encoding.ASCII.GetBytes($"{creds["Email"]}/token:{creds["ApiToken"]}");
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            var response = await http.GetAsync($"https://{creds["Subdomain"]}.zendesk.com/api/v2/users/me.json");
            if (response.IsSuccessStatusCode)
                return new ConnectionTestResult { Success = true, PlatformInfo = "Zendesk baglantisi basarili." };
            return new ConnectionTestResult { Success = false, Error = $"Zendesk API hatasi: {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult { Success = false, Error = $"Baglanti hatasi: {ex.Message}" };
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    private static IntegrationConnectionListDto MapToListDto(IntegrationConnection conn)
    {
        var platform = IntegrationPlatforms.GetById(conn.PlatformTypeId);
        return new IntegrationConnectionListDto
        {
            Id = conn.Id,
            Uid = conn.Uid,
            Name = conn.Name,
            PlatformTypeId = conn.PlatformTypeId,
            PlatformName = platform?.SystemName ?? "Unknown",
            PlatformIcon = platform?.Icon ?? "",
            PlatformCss = platform?.CssClass ?? "",
            IsActive = conn.IsActive,
            IsConnected = conn.IsConnected,
            AutoLogCalls = conn.AutoLogCalls,
            ContactSyncDirectionId = conn.ContactSyncDirectionId,
            LastSyncAt = conn.LastSyncAt,
            LastError = conn.LastError,
            WebhookCount = conn.WebhookSubscriptions.Count,
            ApiKeyCount = conn.ApiKeys.Count,
            CreatedAt = conn.CreatedAt
        };
    }

    private static WebhookSubscriptionDto MapToWebhookDto(WebhookSubscription wh)
    {
        List<string> eventFilter = new();
        if (!string.IsNullOrEmpty(wh.EventFilter))
        {
            try { eventFilter = JsonSerializer.Deserialize<List<string>>(wh.EventFilter) ?? new(); }
            catch { /* gecersiz JSON — bos liste */ }
        }

        return new WebhookSubscriptionDto
        {
            Id = wh.Id,
            Uid = wh.Uid,
            Name = wh.Name,
            TargetUrl = wh.TargetUrl,
            EventFilter = eventFilter,
            IsActive = wh.IsActive,
            IntegrationConnectionId = wh.IntegrationConnectionId,
            IntegrationConnectionName = wh.IntegrationConnection?.Name,
            CreatedAt = wh.CreatedAt
        };
    }

    private static IntegrationApiKeyDto MapToApiKeyDto(IntegrationApiKey key)
    {
        List<string> scopes = new();
        if (!string.IsNullOrEmpty(key.Scopes))
        {
            try { scopes = JsonSerializer.Deserialize<List<string>>(key.Scopes) ?? new(); }
            catch { /* gecersiz JSON */ }
        }

        return new IntegrationApiKeyDto
        {
            Id = key.Id,
            Uid = key.Uid,
            Name = key.Name,
            ApiKeyPrefix = key.ApiKeyPrefix,
            Scopes = scopes,
            ExpiresAt = key.ExpiresAt,
            IsActive = key.IsActive,
            LastUsedAt = key.LastUsedAt,
            CreatedAt = key.CreatedAt
        };
    }

    private static string GenerateWebhookSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return $"whsec_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
    }

    private static string GenerateRandomKey(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var result = new char[length];
        var bytes = RandomNumberGenerator.GetBytes(length);
        for (int i = 0; i < length; i++)
            result[i] = chars[bytes[i] % chars.Length];
        return new string(result);
    }

    /// <summary>API key'i SHA-256 ile hashle (dogrulama icin)</summary>
    public static string HashApiKey(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexStringLower(bytes);
    }
}

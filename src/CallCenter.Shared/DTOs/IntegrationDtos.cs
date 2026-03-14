using System.ComponentModel.DataAnnotations;

namespace CallCenter.Shared.DTOs;

// ═══════════════════════════════════════════════════════════════
// Integration Connection DTO'lari
// ═══════════════════════════════════════════════════════════════

/// <summary>Entegrasyon baglantisi liste gorunumu</summary>
public class IntegrationConnectionListDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PlatformTypeId { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string PlatformIcon { get; set; } = string.Empty;
    public string PlatformCss { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsConnected { get; set; }
    public bool AutoLogCalls { get; set; }
    public int ContactSyncDirectionId { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastError { get; set; }
    public int WebhookCount { get; set; }
    public int ApiKeyCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Entegrasyon baglantisi detay (webhook + api key sayilari dahil)</summary>
public class IntegrationConnectionDetailDto : IntegrationConnectionListDto
{
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSuccess { get; set; }
    public List<WebhookSubscriptionDto> Webhooks { get; set; } = new();
    public List<IntegrationApiKeyDto> ApiKeys { get; set; } = new();
}

/// <summary>Yeni entegrasyon baglantisi olusturma</summary>
public class CreateIntegrationConnectionRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int PlatformTypeId { get; set; }

    /// <summary>Platform ayarlari (Zendesk icin subdomain+email+token, GenericWebhook icin bos olabilir)</summary>
    public Dictionary<string, string>? Credentials { get; set; }

    public bool AutoLogCalls { get; set; } = true;
    public int ContactSyncDirectionId { get; set; }
}

/// <summary>Entegrasyon baglantisi guncelleme</summary>
public class UpdateIntegrationConnectionRequest
{
    [StringLength(200)]
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public bool? AutoLogCalls { get; set; }
    public int? ContactSyncDirectionId { get; set; }
    public Dictionary<string, string>? Credentials { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// OAuth2 DTO'lari
// ═══════════════════════════════════════════════════════════════

/// <summary>OAuth2 akisi baslatma — frontend bu URL'e redirect eder</summary>
public class OAuthInitiateResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

/// <summary>OAuth2 callback parametreleri</summary>
public class OAuthCallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════
// Webhook Subscription DTO'lari
// ═══════════════════════════════════════════════════════════════

/// <summary>Webhook aboneligi gorunumu</summary>
public class WebhookSubscriptionDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public List<string> EventFilter { get; set; } = new();
    public bool IsActive { get; set; }
    public int? IntegrationConnectionId { get; set; }
    public string? IntegrationConnectionName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Yeni webhook aboneligi olusturma</summary>
public class CreateWebhookSubscriptionRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, Url]
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>Dinlenecek event tipleri (bos = hepsi)</summary>
    public List<string>? EventFilter { get; set; }

    /// <summary>Opsiyonel: Hangi entegrasyon baglantisina ait</summary>
    public int? IntegrationConnectionId { get; set; }
}

/// <summary>Webhook aboneligi guncelleme</summary>
public class UpdateWebhookSubscriptionRequest
{
    [StringLength(200)]
    public string? Name { get; set; }

    [Url]
    public string? TargetUrl { get; set; }

    public List<string>? EventFilter { get; set; }
    public bool? IsActive { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// Webhook Delivery DTO'lari
// ═══════════════════════════════════════════════════════════════

/// <summary>Webhook gonderim log gorunumu</summary>
public class WebhookDeliveryDto
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int Attempt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// API Key DTO'lari
// ═══════════════════════════════════════════════════════════════

/// <summary>API key gorunumu (hash gizli, sadece prefix gosterilir)</summary>
public class IntegrationApiKeyDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKeyPrefix { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>API key olusturma sonucu — key sadece bir kez gosterilir!</summary>
public class CreateApiKeyResponse
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>API key plain text — SADECE olusturma aninda gosterilir, sonra bir daha okunamaz!</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string ApiKeyPrefix { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
}

/// <summary>Yeni API key olusturma</summary>
public class CreateApiKeyRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int IntegrationConnectionId { get; set; }

    /// <summary>Yetki kapsamlari: ["calls:read","calls:write","contacts:read","contacts:write","agents:read","tickets:write"]</summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>Son kullanim tarihi (null = suresiz)</summary>
    public DateTime? ExpiresAt { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// Webhook Event Payload DTO'lari (dis sisteme gonderilen)
// ═══════════════════════════════════════════════════════════════

/// <summary>Standart webhook event payload</summary>
public class WebhookEventPayload
{
    public string Event { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string CustomerUid { get; set; } = string.Empty;
    public object? Data { get; set; }
}

/// <summary>Cagri event data</summary>
public class WebhookCallEventData
{
    public string CallUid { get; set; } = string.Empty;
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? DurationSeconds { get; set; }
    public string? AgentName { get; set; }
    public string? QueueName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool RecordingAvailable { get; set; }
}

/// <summary>Agent status event data</summary>
public class WebhookAgentEventData
{
    public string AgentUid { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public DateTime Timestamp { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// Connector DTO'lari (platform-specific islemler icin)
// ═══════════════════════════════════════════════════════════════

/// <summary>Dis sistemdeki kisi bilgisi</summary>
public class ExternalContactDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? ExternalUrl { get; set; }
}

/// <summary>Cagri aktivitesi loglama (dis sisteme gonderilecek)</summary>
public class CallActivityPayload
{
    public string CallUid { get; set; } = string.Empty;
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string? QueueName { get; set; }
    public string? Notes { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime EndedAt { get; set; }
    public string? ExternalContactId { get; set; }
}

/// <summary>Baglanti testi sonucu</summary>
public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? PlatformInfo { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// Integration API Gateway DTO'lari (dis sistemlerin bize gonderecegi)
// ═══════════════════════════════════════════════════════════════

/// <summary>Click-to-dial istegi (dis sistemden gelen arama baslatma)</summary>
public class ClickToDialRequest
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Aramayi yapacak agent'in Uid'si (opsiyonel — yoksa ilk musait agent)</summary>
    public Guid? AgentUid { get; set; }
}

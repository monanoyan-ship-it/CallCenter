namespace CallCenter.Shared.Entities;

/// <summary>
/// Webhook event aboneligi.
/// Musteri belirli olaylari (call.started, call.ended vb.) dinlemek icin URL kaydeder.
/// HMAC-SHA256 imzalama ile guvenlik saglanir.
/// </summary>
public class WebhookSubscription
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    // Hangi musteriye ait
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Opsiyonel: Hangi entegrasyon baglantisina ait (null = genel webhook)
    public int? IntegrationConnectionId { get; set; }
    public IntegrationConnection? IntegrationConnection { get; set; }

    /// <summary>Abonelik adi: "Salesforce Webhook", "Custom CRM Callback"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Webhook POST yapilacak URL (HTTPS zorunlu production'da)</summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 imzalama anahtari (X-Webhook-Signature header)</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Dinlenecek event tipleri (JSON array).
    /// Ornek: ["call.started","call.ended","agent.status_changed"]
    /// null = tum eventler
    /// </summary>
    public string? EventFilter { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
}

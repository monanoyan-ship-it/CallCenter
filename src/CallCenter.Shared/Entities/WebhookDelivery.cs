namespace CallCenter.Shared.Entities;

/// <summary>
/// Webhook gonderim loglari — audit + retry mekanizmasi.
/// Her event gonderimi icin bir kayit olusturulur.
/// Basarisiz gonderimlerde retry zamanlayicisi devreye girer.
/// </summary>
public class WebhookDelivery
{
    /// <summary>bigint PK — yuksek hacimli tablo</summary>
    public long Id { get; set; }

    public int WebhookSubscriptionId { get; set; }
    public WebhookSubscription WebhookSubscription { get; set; } = null!;

    /// <summary>Event tipi: "call.started", "call.ended" vb.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Idempotency key — ayni event'in tekrar islenmesini onler</summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>Gonderilen JSON payload</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>HTTP response status code (null = henuz gonderilmedi veya baglanti hatasi)</summary>
    public int? StatusCode { get; set; }

    /// <summary>HTTP response body (hata durumunda debug icin)</summary>
    public string? ResponseBody { get; set; }

    /// <summary>Hata mesaji (timeout, DNS, connection refused vb.)</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Kacinci deneme (1-5)</summary>
    public int Attempt { get; set; } = 1;

    /// <summary>Sonraki retry zamani (null = retry yok veya basarili)</summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>Basarili gonderim zamani</summary>
    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

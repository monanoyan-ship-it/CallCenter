namespace CallCenter.Shared.Entities;

/// <summary>
/// Platform kullanicisinin (mobil/web) push notification token'i.
/// Bir PlatformUser birden fazla cihaz icin token kaydedebilir.
/// </summary>
public class PlatformPushToken
{
    public int Id { get; set; }

    public int PlatformUserId { get; set; }
    public PlatformUser? PlatformUser { get; set; }

    /// <summary>FCM/APNs token (uzun string).</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>"ios", "android", "web"</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>Cihaz tanimlayici (opsiyonel, ayni cihazda token degisirse update icin).</summary>
    public string? DeviceId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

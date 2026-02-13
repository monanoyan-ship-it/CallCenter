namespace CallCenter.Shared.Entities;

/// <summary>
/// KVKK / BDDK uyumlu audit log entity.
/// Tum CRUD, auth ve onemli islemleri kaydeder.
/// Min 3 yil saklanacak (KVKK zorunlulugu).
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>Olayın kategorisi: Auth, User, Customer, Queue, SipAccount, Call, Setting, Portal, Permission, Organization</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Olay tipi: Login, LoginFailed, Logout, Create, Update, Delete, Deactivate, PasswordReset, Refresh, Revoke, Sync, AssignAgent, RemoveAgent, StatusChange</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>İşlemi yapan kullanıcının ID'si (null: anonim, ör. başarısız login)</summary>
    public int? UserId { get; set; }

    /// <summary>İşlemi yapan kullanıcının adı (snapshot — user silinse bile log okunabilir)</summary>
    public string? UserName { get; set; }

    /// <summary>Etkilenen entity tipi: User, Customer, Queue, SipAccount, CallRecord, SystemSetting, vb.</summary>
    public string? EntityType { get; set; }

    /// <summary>Etkilenen entity'nin ID'si (string — int veya Guid olabilir)</summary>
    public string? EntityId { get; set; }

    /// <summary>Kısa açıklama: "Müşteri 'Acme Corp' oluşturuldu", "Kullanıcı 'ali' silindi" gibi</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Eski değerler (JSON) — Update/Delete işlemlerinde</summary>
    public string? OldValues { get; set; }

    /// <summary>Yeni değerler (JSON) — Create/Update işlemlerinde</summary>
    public string? NewValues { get; set; }

    /// <summary>İstek yapan IP adresi</summary>
    public string? IpAddress { get; set; }

    /// <summary>User-Agent header bilgisi</summary>
    public string? UserAgent { get; set; }

    /// <summary>Olay zamanı (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Müşteri ID'si (çoklu müşteri izolasyonu için)</summary>
    public int? CustomerId { get; set; }

    // Navigation property'ler
    public User? User { get; set; }
    public Customer? Customer { get; set; }
}

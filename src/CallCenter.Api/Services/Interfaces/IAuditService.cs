namespace CallCenter.Api.Services.Interfaces;

/// <summary>
/// KVKK/BDDK uyumlu audit log servisi.
/// Controller'lardan tek satirla cagrilir, async kayit yapar.
/// </summary>
public interface IAuditService
{
    /// <summary>Genel audit log kaydi</summary>
    Task LogAsync(AuditEntry entry);

    /// <summary>Auth olaylari icin kolaylik metodu</summary>
    Task LogAuthAsync(string action, string description, int? userId = null, string? userName = null, string? ipAddress = null, string? userAgent = null);

    /// <summary>CRUD olaylari icin kolaylik metodu</summary>
    Task LogCrudAsync(string action, string entityType, string? entityId, string description,
        int? userId = null, string? userName = null,
        string? oldValues = null, string? newValues = null,
        int? customerId = null, string? ipAddress = null, string? userAgent = null);
}

/// <summary>
/// Audit log kaydi icin helper DTO.
/// Controller → AuditService iletisiminde kullanilir.
/// </summary>
public class AuditEntry
{
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public int? CustomerId { get; set; }
}

using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.Services;

/// <summary>
/// KVKK/BDDK uyumlu audit log servisi.
/// Her CRUD ve auth islemini AuditLogs tablosuna kaydeder.
/// Scoped lifetime — her request icin yeni instance.
/// </summary>
public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(AuditEntry entry)
    {
        var log = new AuditLog
        {
            Category = entry.Category,
            Action = entry.Action,
            UserId = entry.UserId,
            UserName = entry.UserName,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Description = entry.Description,
            OldValues = entry.OldValues,
            NewValues = entry.NewValues,
            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,
            CustomerId = entry.CustomerId,
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task LogAuthAsync(string action, string description,
        int? userId = null, string? userName = null,
        string? ipAddress = null, string? userAgent = null)
    {
        await LogAsync(new AuditEntry
        {
            Category = "Auth",
            Action = action,
            UserId = userId,
            UserName = userName,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });
    }

    public async Task LogCrudAsync(string action, string entityType, string? entityId, string description,
        int? userId = null, string? userName = null,
        string? oldValues = null, string? newValues = null,
        int? customerId = null, string? ipAddress = null, string? userAgent = null)
    {
        await LogAsync(new AuditEntry
        {
            Category = entityType ?? "System",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            UserName = userName,
            Description = description,
            OldValues = oldValues,
            NewValues = newValues,
            CustomerId = customerId,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });
    }
}

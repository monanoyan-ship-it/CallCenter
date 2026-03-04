using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.Factories;

public class AuditFactory : IAuditFactory
{
    private readonly IAuditLogEntityService _auditEs;
    private readonly IUnitOfWork _uow;

    public AuditFactory(IAuditLogEntityService auditEs, IUnitOfWork uow)
    {
        _auditEs = auditEs;
        _uow = uow;
    }

    public async Task LogAuthAsync(string action, string description,
        int? userId = null, string? userName = null,
        string? ipAddress = null, string? userAgent = null)
    {
        var log = new AuditLog
        {
            Category = "Auth",
            Action = action,
            UserId = userId,
            UserName = userName,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };
        _auditEs.Add(log);
        await _uow.SaveChangesAsync();
    }

    public async Task LogCrudAsync(string action, string entityType, string? entityId, string description,
        int? userId = null, string? userName = null,
        string? oldValues = null, string? newValues = null,
        int? customerId = null, string? ipAddress = null, string? userAgent = null)
    {
        var log = new AuditLog
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
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };
        _auditEs.Add(log);
        await _uow.SaveChangesAsync();
    }
}

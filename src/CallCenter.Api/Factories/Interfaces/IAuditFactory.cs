namespace CallCenter.Api.Factories.Interfaces;

public interface IAuditFactory
{
    Task LogAuthAsync(string action, string description, int? userId = null, string? userName = null, string? ipAddress = null, string? userAgent = null);
    Task LogCrudAsync(string action, string entityType, string? entityId, string description, int? userId = null, string? userName = null, string? oldValues = null, string? newValues = null, int? customerId = null, string? ipAddress = null, string? userAgent = null);
}

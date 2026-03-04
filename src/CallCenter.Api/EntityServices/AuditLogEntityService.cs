using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class AuditLogEntityService : IAuditLogEntityService
{
    private readonly AppDbContext _db;

    public AuditLogEntityService(AppDbContext db) => _db = db;

    public IQueryable<AuditLog> GetAllQueryable()
        => _db.AuditLogs.AsQueryable();

    public void Add(AuditLog entity) => _db.AuditLogs.Add(entity);
}

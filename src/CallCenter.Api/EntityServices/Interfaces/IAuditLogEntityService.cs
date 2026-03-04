using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IAuditLogEntityService
{
    IQueryable<AuditLog> GetAllQueryable();
    void Add(AuditLog entity);
}

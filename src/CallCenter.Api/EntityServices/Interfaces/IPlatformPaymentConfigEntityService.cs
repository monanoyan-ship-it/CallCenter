using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IPlatformPaymentConfigEntityService
{
    IQueryable<PlatformPaymentConfig> GetAllQueryable();
    Task<PlatformPaymentConfig?> GetByIdAsync(int id);
    void Add(PlatformPaymentConfig entity);
    void Update(PlatformPaymentConfig entity);
    void Remove(PlatformPaymentConfig entity);
}

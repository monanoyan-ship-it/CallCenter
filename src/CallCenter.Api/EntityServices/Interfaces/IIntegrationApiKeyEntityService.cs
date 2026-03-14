using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IIntegrationApiKeyEntityService
{
    Task<IntegrationApiKey?> GetByIdAsync(int id);
    Task<IntegrationApiKey?> GetByUidAsync(Guid uid);
    /// <summary>API key hash ile dogrulama (middleware icin)</summary>
    Task<IntegrationApiKey?> GetByHashAsync(string apiKeyHash);
    IQueryable<IntegrationApiKey> GetByConnectionQueryable(int connectionId);
    IQueryable<IntegrationApiKey> GetByCustomerQueryable(int customerId);
    void Add(IntegrationApiKey entity);
    void Update(IntegrationApiKey entity);
    void Remove(IntegrationApiKey entity);
}

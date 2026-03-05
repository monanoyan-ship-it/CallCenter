using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IServiceDefinitionEntityService
{
    Task<ServiceDefinition?> GetByIdAsync(int id);
    IQueryable<ServiceDefinition> GetAllQueryable();
    void Add(ServiceDefinition entity);
    void Update(ServiceDefinition entity);
}

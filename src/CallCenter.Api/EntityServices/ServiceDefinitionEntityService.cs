using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class ServiceDefinitionEntityService : IServiceDefinitionEntityService
{
    private readonly AppDbContext _db;

    public ServiceDefinitionEntityService(AppDbContext db) => _db = db;

    public Task<ServiceDefinition?> GetByIdAsync(int id)
        => _db.ServiceDefinitions.FindAsync(id).AsTask();

    public IQueryable<ServiceDefinition> GetAllQueryable()
        => _db.ServiceDefinitions.AsQueryable();

    public void Add(ServiceDefinition entity) => _db.ServiceDefinitions.Add(entity);
    public void Update(ServiceDefinition entity) => _db.ServiceDefinitions.Update(entity);
}

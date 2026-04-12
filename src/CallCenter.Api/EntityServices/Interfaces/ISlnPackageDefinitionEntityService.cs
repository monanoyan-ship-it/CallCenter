using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnPackageDefinitionEntityService
{
    IQueryable<SlnPackageDefinition> GetAllQueryable();
    Task<SlnPackageDefinition?> GetByIdAsync(int id);
    void Add(SlnPackageDefinition entity);
    void Update(SlnPackageDefinition entity);
    void Remove(SlnPackageDefinition entity);
}

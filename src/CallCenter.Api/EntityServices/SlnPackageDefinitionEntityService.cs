using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnPackageDefinitionEntityService : ISlnPackageDefinitionEntityService
{
    private readonly AppDbContext _db;

    public SlnPackageDefinitionEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnPackageDefinition> GetAllQueryable()
        => _db.SlnPackageDefinitions.AsQueryable();

    public Task<SlnPackageDefinition?> GetByIdAsync(int id)
        => _db.SlnPackageDefinitions.FindAsync(id).AsTask();

    public void Add(SlnPackageDefinition entity) => _db.SlnPackageDefinitions.Add(entity);
    public void Update(SlnPackageDefinition entity) => _db.SlnPackageDefinitions.Update(entity);
    public void Remove(SlnPackageDefinition entity) => _db.SlnPackageDefinitions.Remove(entity);
}

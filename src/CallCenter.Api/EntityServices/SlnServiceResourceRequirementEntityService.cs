using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnServiceResourceRequirementEntityService : ISlnServiceResourceRequirementEntityService
{
    private readonly AppDbContext _db;

    public SlnServiceResourceRequirementEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnServiceResourceRequirement> GetAllQueryable()
        => _db.SlnServiceResourceRequirements.AsQueryable();

    public Task<SlnServiceResourceRequirement?> GetByIdAsync(int id)
        => _db.SlnServiceResourceRequirements.FindAsync(id).AsTask();

    public void Add(SlnServiceResourceRequirement entity) => _db.SlnServiceResourceRequirements.Add(entity);
    public void Remove(SlnServiceResourceRequirement entity) => _db.SlnServiceResourceRequirements.Remove(entity);
    public void RemoveRange(IEnumerable<SlnServiceResourceRequirement> entities) => _db.SlnServiceResourceRequirements.RemoveRange(entities);
}

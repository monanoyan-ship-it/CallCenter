using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnServiceResourceRequirementEntityService
{
    IQueryable<SlnServiceResourceRequirement> GetAllQueryable();
    Task<SlnServiceResourceRequirement?> GetByIdAsync(int id);
    void Add(SlnServiceResourceRequirement entity);
    void Remove(SlnServiceResourceRequirement entity);
    void RemoveRange(IEnumerable<SlnServiceResourceRequirement> entities);
}

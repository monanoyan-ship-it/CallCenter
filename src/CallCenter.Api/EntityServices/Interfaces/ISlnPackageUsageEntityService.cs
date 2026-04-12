using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnPackageUsageEntityService
{
    IQueryable<SlnPackageUsage> GetAllQueryable();
    Task<SlnPackageUsage?> GetByIdAsync(int id);
    void Add(SlnPackageUsage entity);
    void Update(SlnPackageUsage entity);
    void Remove(SlnPackageUsage entity);
}

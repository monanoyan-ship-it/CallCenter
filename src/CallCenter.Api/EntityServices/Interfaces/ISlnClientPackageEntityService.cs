using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnClientPackageEntityService
{
    IQueryable<SlnClientPackage> GetAllQueryable();
    Task<SlnClientPackage?> GetByIdAsync(int id);
    void Add(SlnClientPackage entity);
    void Update(SlnClientPackage entity);
    void Remove(SlnClientPackage entity);
}

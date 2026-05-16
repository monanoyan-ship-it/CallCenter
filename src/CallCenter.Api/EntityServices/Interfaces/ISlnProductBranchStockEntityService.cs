using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnProductBranchStockEntityService
{
    IQueryable<SlnProductBranchStock> GetAllQueryable();
    Task<SlnProductBranchStock?> GetByIdAsync(int id);
    void Add(SlnProductBranchStock entity);
    void Update(SlnProductBranchStock entity);
    void Remove(SlnProductBranchStock entity);
}

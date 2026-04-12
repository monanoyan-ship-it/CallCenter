using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnPersonnelServicePriceEntityService
{
    IQueryable<SlnPersonnelServicePrice> GetAllQueryable();
    Task<SlnPersonnelServicePrice?> GetByIdAsync(int id);
    void Add(SlnPersonnelServicePrice entity);
    void Update(SlnPersonnelServicePrice entity);
    void Remove(SlnPersonnelServicePrice entity);
}

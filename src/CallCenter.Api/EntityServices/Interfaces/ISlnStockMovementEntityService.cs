using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnStockMovementEntityService
{
    IQueryable<SlnStockMovement> GetAllQueryable();
    Task<SlnStockMovement?> GetByIdAsync(int id);
    void Add(SlnStockMovement entity);
    void Remove(SlnStockMovement entity);
}

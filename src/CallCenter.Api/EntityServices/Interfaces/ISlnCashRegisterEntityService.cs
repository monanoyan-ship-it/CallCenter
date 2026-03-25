using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnCashRegisterEntityService
{
    IQueryable<SlnCashRegister> GetAllQueryable();
    Task<SlnCashRegister?> GetByIdAsync(int id);
    void Add(SlnCashRegister entity);
    void Update(SlnCashRegister entity);
    void Remove(SlnCashRegister entity);
}

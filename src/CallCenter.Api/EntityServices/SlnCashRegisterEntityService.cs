using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnCashRegisterEntityService : ISlnCashRegisterEntityService
{
    private readonly AppDbContext _db;

    public SlnCashRegisterEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnCashRegister> GetAllQueryable()
        => _db.SlnCashRegisters.AsQueryable();

    public Task<SlnCashRegister?> GetByIdAsync(int id)
        => _db.SlnCashRegisters.FindAsync(id).AsTask();

    public void Add(SlnCashRegister entity) => _db.SlnCashRegisters.Add(entity);
    public void Update(SlnCashRegister entity) => _db.SlnCashRegisters.Update(entity);
    public void Remove(SlnCashRegister entity) => _db.SlnCashRegisters.Remove(entity);
}

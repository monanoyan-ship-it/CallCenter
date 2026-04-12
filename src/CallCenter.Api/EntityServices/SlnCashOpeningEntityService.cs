using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnCashOpeningEntityService : ISlnCashOpeningEntityService
{
    private readonly AppDbContext _db;

    public SlnCashOpeningEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnCashOpening> GetAllQueryable()
        => _db.SlnCashOpenings.AsQueryable();

    public Task<SlnCashOpening?> GetByIdAsync(int id)
        => _db.SlnCashOpenings.FindAsync(id).AsTask();

    public void Add(SlnCashOpening entity) => _db.SlnCashOpenings.Add(entity);
    public void Update(SlnCashOpening entity) => _db.SlnCashOpenings.Update(entity);
    public void Remove(SlnCashOpening entity) => _db.SlnCashOpenings.Remove(entity);
}

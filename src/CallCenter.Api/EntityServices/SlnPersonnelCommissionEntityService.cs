using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnPersonnelCommissionEntityService : ISlnPersonnelCommissionEntityService
{
    private readonly AppDbContext _db;

    public SlnPersonnelCommissionEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnPersonnelCommission> GetAllQueryable()
        => _db.SlnPersonnelCommissions.AsQueryable();

    public Task<SlnPersonnelCommission?> GetByIdAsync(int id)
        => _db.SlnPersonnelCommissions.FindAsync(id).AsTask();

    public void Add(SlnPersonnelCommission entity) => _db.SlnPersonnelCommissions.Add(entity);
    public void Update(SlnPersonnelCommission entity) => _db.SlnPersonnelCommissions.Update(entity);
    public void Remove(SlnPersonnelCommission entity) => _db.SlnPersonnelCommissions.Remove(entity);
}

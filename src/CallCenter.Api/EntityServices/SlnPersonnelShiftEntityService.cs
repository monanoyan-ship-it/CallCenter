using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnPersonnelShiftEntityService : ISlnPersonnelShiftEntityService
{
    private readonly AppDbContext _db;
    public SlnPersonnelShiftEntityService(AppDbContext db) => _db = db;
    public IQueryable<SlnPersonnelShift> GetAllQueryable() => _db.SlnPersonnelShifts.AsQueryable();
    public Task<SlnPersonnelShift?> GetByIdAsync(int id) => _db.SlnPersonnelShifts.FindAsync(id).AsTask();
    public void Add(SlnPersonnelShift entity) => _db.SlnPersonnelShifts.Add(entity);
    public void Update(SlnPersonnelShift entity) => _db.SlnPersonnelShifts.Update(entity);
    public void Remove(SlnPersonnelShift entity) => _db.SlnPersonnelShifts.Remove(entity);
}

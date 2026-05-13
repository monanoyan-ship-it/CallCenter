using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnPersonnelLeaveEntityService : ISlnPersonnelLeaveEntityService
{
    private readonly AppDbContext _db;
    public SlnPersonnelLeaveEntityService(AppDbContext db) => _db = db;
    public IQueryable<SlnPersonnelLeave> GetAllQueryable() => _db.SlnPersonnelLeaves.AsQueryable();
    public Task<SlnPersonnelLeave?> GetByIdAsync(int id) => _db.SlnPersonnelLeaves.FindAsync(id).AsTask();
    public void Add(SlnPersonnelLeave entity) => _db.SlnPersonnelLeaves.Add(entity);
    public void Update(SlnPersonnelLeave entity) => _db.SlnPersonnelLeaves.Update(entity);
    public void Remove(SlnPersonnelLeave entity) => _db.SlnPersonnelLeaves.Remove(entity);
}

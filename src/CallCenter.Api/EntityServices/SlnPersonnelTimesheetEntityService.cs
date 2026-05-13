using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnPersonnelTimesheetEntityService : ISlnPersonnelTimesheetEntityService
{
    private readonly AppDbContext _db;
    public SlnPersonnelTimesheetEntityService(AppDbContext db) => _db = db;
    public IQueryable<SlnPersonnelTimesheet> GetAllQueryable() => _db.SlnPersonnelTimesheets.AsQueryable();
    public Task<SlnPersonnelTimesheet?> GetByIdAsync(int id) => _db.SlnPersonnelTimesheets.FindAsync(id).AsTask();
    public void Add(SlnPersonnelTimesheet entity) => _db.SlnPersonnelTimesheets.Add(entity);
    public void Update(SlnPersonnelTimesheet entity) => _db.SlnPersonnelTimesheets.Update(entity);
    public void Remove(SlnPersonnelTimesheet entity) => _db.SlnPersonnelTimesheets.Remove(entity);
}

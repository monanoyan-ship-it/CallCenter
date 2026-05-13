using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnPayrollEntityService : ISlnPayrollEntityService
{
    private readonly AppDbContext _db;
    public SlnPayrollEntityService(AppDbContext db) => _db = db;
    public IQueryable<SlnPayroll> GetAllQueryable() => _db.SlnPayrolls.AsQueryable();
    public Task<SlnPayroll?> GetByIdAsync(int id) => _db.SlnPayrolls.FindAsync(id).AsTask();
    public void Add(SlnPayroll entity) => _db.SlnPayrolls.Add(entity);
    public void Update(SlnPayroll entity) => _db.SlnPayrolls.Update(entity);
    public void Remove(SlnPayroll entity) => _db.SlnPayrolls.Remove(entity);
}

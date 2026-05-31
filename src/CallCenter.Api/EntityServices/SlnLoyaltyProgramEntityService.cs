using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnLoyaltyProgramEntityService : ISlnLoyaltyProgramEntityService
{
    private readonly AppDbContext _db;

    public SlnLoyaltyProgramEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnLoyaltyProgram> GetAllQueryable() => _db.SlnLoyaltyPrograms.AsQueryable();
    public Task<SlnLoyaltyProgram?> GetByIdAsync(int id) => _db.SlnLoyaltyPrograms.FindAsync(id).AsTask();
    public void Add(SlnLoyaltyProgram entity) => _db.SlnLoyaltyPrograms.Add(entity);
    public void Update(SlnLoyaltyProgram entity) => _db.SlnLoyaltyPrograms.Update(entity);
    public void Remove(SlnLoyaltyProgram entity) => _db.SlnLoyaltyPrograms.Remove(entity);
}

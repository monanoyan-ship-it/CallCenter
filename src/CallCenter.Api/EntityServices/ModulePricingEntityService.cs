using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class ModulePricingEntityService : IModulePricingEntityService
{
    private readonly AppDbContext _db;

    public ModulePricingEntityService(AppDbContext db) => _db = db;

    public Task<List<ModulePricing>> GetAllAsync()
        => _db.ModulePricings.ToListAsync();

    public Task<ModulePricing?> GetByModuleIdAsync(int moduleId)
        => _db.ModulePricings.FirstOrDefaultAsync(p => p.ModuleId == moduleId);

    public void Add(ModulePricing entity) => _db.ModulePricings.Add(entity);
    public void Update(ModulePricing entity) => _db.ModulePricings.Update(entity);
}

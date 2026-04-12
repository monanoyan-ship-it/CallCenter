using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnWinbackRuleEntityService : ISlnWinbackRuleEntityService
{
    private readonly AppDbContext _db;

    public SlnWinbackRuleEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnWinbackRule> GetAllQueryable()
        => _db.SlnWinbackRules.AsQueryable();

    public Task<SlnWinbackRule?> GetByIdAsync(int id)
        => _db.SlnWinbackRules.FindAsync(id).AsTask();

    public void Add(SlnWinbackRule entity) => _db.SlnWinbackRules.Add(entity);
    public void Update(SlnWinbackRule entity) => _db.SlnWinbackRules.Update(entity);
    public void Remove(SlnWinbackRule entity) => _db.SlnWinbackRules.Remove(entity);
}

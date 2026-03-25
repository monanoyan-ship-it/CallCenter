using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnFormulaEntityService : ISlnFormulaEntityService
{
    private readonly AppDbContext _db;

    public SlnFormulaEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnFormula> GetAllQueryable()
        => _db.SlnFormulas.AsQueryable();

    public Task<SlnFormula?> GetByIdAsync(int id)
        => _db.SlnFormulas.FindAsync(id).AsTask();

    public void Add(SlnFormula entity) => _db.SlnFormulas.Add(entity);
    public void Update(SlnFormula entity) => _db.SlnFormulas.Update(entity);
    public void Remove(SlnFormula entity) => _db.SlnFormulas.Remove(entity);
}

using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnServiceComboEntityService : ISlnServiceComboEntityService
{
    private readonly AppDbContext _db;

    public SlnServiceComboEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnServiceCombo> GetAllQueryable()
        => _db.SlnServiceCombos.AsQueryable();

    public Task<SlnServiceCombo?> GetByIdAsync(int id)
        => _db.SlnServiceCombos.FindAsync(id).AsTask();

    public void Add(SlnServiceCombo entity) => _db.SlnServiceCombos.Add(entity);
    public void Update(SlnServiceCombo entity) => _db.SlnServiceCombos.Update(entity);
    public void Remove(SlnServiceCombo entity) => _db.SlnServiceCombos.Remove(entity);
}

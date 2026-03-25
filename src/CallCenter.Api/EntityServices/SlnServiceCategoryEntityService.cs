using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnServiceCategoryEntityService : ISlnServiceCategoryEntityService
{
    private readonly AppDbContext _db;

    public SlnServiceCategoryEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnServiceCategory> GetAllQueryable()
        => _db.SlnServiceCategories.AsQueryable();

    public Task<SlnServiceCategory?> GetByIdAsync(int id)
        => _db.SlnServiceCategories.FindAsync(id).AsTask();

    public void Add(SlnServiceCategory entity) => _db.SlnServiceCategories.Add(entity);
    public void Update(SlnServiceCategory entity) => _db.SlnServiceCategories.Update(entity);
    public void Remove(SlnServiceCategory entity) => _db.SlnServiceCategories.Remove(entity);
}

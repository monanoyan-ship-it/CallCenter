using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnProductCategoryEntityService : ISlnProductCategoryEntityService
{
    private readonly AppDbContext _db;

    public SlnProductCategoryEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnProductCategory> GetAllQueryable()
        => _db.SlnProductCategories.AsQueryable();

    public Task<SlnProductCategory?> GetByIdAsync(int id)
        => _db.SlnProductCategories.FindAsync(id).AsTask();

    public void Add(SlnProductCategory entity) => _db.SlnProductCategories.Add(entity);
    public void Update(SlnProductCategory entity) => _db.SlnProductCategories.Update(entity);
    public void Remove(SlnProductCategory entity) => _db.SlnProductCategories.Remove(entity);
}

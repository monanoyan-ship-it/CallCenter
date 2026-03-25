using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnExpenseCategoryEntityService : ISlnExpenseCategoryEntityService
{
    private readonly AppDbContext _db;

    public SlnExpenseCategoryEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnExpenseCategory> GetAllQueryable()
        => _db.SlnExpenseCategories.AsQueryable();

    public Task<SlnExpenseCategory?> GetByIdAsync(int id)
        => _db.SlnExpenseCategories.FindAsync(id).AsTask();

    public void Add(SlnExpenseCategory entity) => _db.SlnExpenseCategories.Add(entity);
    public void Update(SlnExpenseCategory entity) => _db.SlnExpenseCategories.Update(entity);
    public void Remove(SlnExpenseCategory entity) => _db.SlnExpenseCategories.Remove(entity);
}

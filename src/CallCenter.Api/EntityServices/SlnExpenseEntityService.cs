using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnExpenseEntityService : ISlnExpenseEntityService
{
    private readonly AppDbContext _db;

    public SlnExpenseEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnExpense> GetAllQueryable()
        => _db.SlnExpenses.AsQueryable();

    public Task<SlnExpense?> GetByIdAsync(int id)
        => _db.SlnExpenses.FindAsync(id).AsTask();

    public void Add(SlnExpense entity) => _db.SlnExpenses.Add(entity);
    public void Update(SlnExpense entity) => _db.SlnExpenses.Update(entity);
    public void Remove(SlnExpense entity) => _db.SlnExpenses.Remove(entity);
}

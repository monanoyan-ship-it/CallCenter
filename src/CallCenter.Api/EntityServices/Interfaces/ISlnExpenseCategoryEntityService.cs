using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnExpenseCategoryEntityService
{
    IQueryable<SlnExpenseCategory> GetAllQueryable();
    Task<SlnExpenseCategory?> GetByIdAsync(int id);
    void Add(SlnExpenseCategory entity);
    void Update(SlnExpenseCategory entity);
    void Remove(SlnExpenseCategory entity);
}

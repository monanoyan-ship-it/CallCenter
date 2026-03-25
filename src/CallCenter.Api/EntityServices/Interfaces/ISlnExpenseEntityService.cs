using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnExpenseEntityService
{
    IQueryable<SlnExpense> GetAllQueryable();
    Task<SlnExpense?> GetByIdAsync(int id);
    void Add(SlnExpense entity);
    void Update(SlnExpense entity);
    void Remove(SlnExpense entity);
}

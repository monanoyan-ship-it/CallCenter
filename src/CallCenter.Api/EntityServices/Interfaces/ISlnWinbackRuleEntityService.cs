using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnWinbackRuleEntityService
{
    IQueryable<SlnWinbackRule> GetAllQueryable();
    Task<SlnWinbackRule?> GetByIdAsync(int id);
    void Add(SlnWinbackRule entity);
    void Update(SlnWinbackRule entity);
    void Remove(SlnWinbackRule entity);
}

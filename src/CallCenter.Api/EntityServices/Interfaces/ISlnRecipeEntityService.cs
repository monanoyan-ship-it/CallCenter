using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnRecipeEntityService
{
    IQueryable<SlnRecipe> GetAllQueryable();
    Task<SlnRecipe?> GetByIdAsync(int id);
    void Add(SlnRecipe entity);
    void Update(SlnRecipe entity);
    void Remove(SlnRecipe entity);
}

public interface ISlnRecipeItemEntityService
{
    IQueryable<SlnRecipeItem> GetAllQueryable();
    void Add(SlnRecipeItem entity);
    void Remove(SlnRecipeItem entity);
    void RemoveRange(IEnumerable<SlnRecipeItem> entities);
}

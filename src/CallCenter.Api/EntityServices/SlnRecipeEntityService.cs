using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnRecipeEntityService : ISlnRecipeEntityService
{
    private readonly AppDbContext _db;
    public SlnRecipeEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnRecipe> GetAllQueryable() => _db.SlnRecipes.AsQueryable();
    public Task<SlnRecipe?> GetByIdAsync(int id) => _db.SlnRecipes.FindAsync(id).AsTask();
    public void Add(SlnRecipe entity) => _db.SlnRecipes.Add(entity);
    public void Update(SlnRecipe entity) => _db.SlnRecipes.Update(entity);
    public void Remove(SlnRecipe entity) => _db.SlnRecipes.Remove(entity);
}

public class SlnRecipeItemEntityService : ISlnRecipeItemEntityService
{
    private readonly AppDbContext _db;
    public SlnRecipeItemEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnRecipeItem> GetAllQueryable() => _db.SlnRecipeItems.AsQueryable();
    public void Add(SlnRecipeItem entity) => _db.SlnRecipeItems.Add(entity);
    public void Remove(SlnRecipeItem entity) => _db.SlnRecipeItems.Remove(entity);
    public void RemoveRange(IEnumerable<SlnRecipeItem> entities) => _db.SlnRecipeItems.RemoveRange(entities);
}

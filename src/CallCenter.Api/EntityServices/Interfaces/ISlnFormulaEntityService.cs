using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnFormulaEntityService
{
    IQueryable<SlnFormula> GetAllQueryable();
    Task<SlnFormula?> GetByIdAsync(int id);
    void Add(SlnFormula entity);
    void Update(SlnFormula entity);
    void Remove(SlnFormula entity);
}

using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnServiceComboEntityService
{
    IQueryable<SlnServiceCombo> GetAllQueryable();
    Task<SlnServiceCombo?> GetByIdAsync(int id);
    void Add(SlnServiceCombo entity);
    void Update(SlnServiceCombo entity);
    void Remove(SlnServiceCombo entity);
}

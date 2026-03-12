using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IQualityChecklistEntityService
{
    Task<QualityChecklist?> GetByIdAsync(int id);
    Task<QualityChecklist?> GetByUidAsync(Guid uid);
    IQueryable<QualityChecklist> GetAllQueryable();
    void Add(QualityChecklist entity);
    void Update(QualityChecklist entity);
    void Delete(QualityChecklist entity);
}

using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmQualityChecklistEntityService
{
    Task<CrmQualityChecklist?> GetByIdAsync(int id);
    Task<CrmQualityChecklist?> GetByUidAsync(Guid uid);
    IQueryable<CrmQualityChecklist> GetAllQueryable();
    void Add(CrmQualityChecklist entity);
    void Update(CrmQualityChecklist entity);
    void Delete(CrmQualityChecklist entity);
}

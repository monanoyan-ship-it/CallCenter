using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IDataSubjectRequestEntityService
{
    IQueryable<DataSubjectRequest> GetAllQueryable();
    Task<DataSubjectRequest?> GetByIdAsync(int id);
    Task<DataSubjectRequest?> GetByUidAsync(Guid uid);
    void Add(DataSubjectRequest entity);
    void Update(DataSubjectRequest entity);
    void Remove(DataSubjectRequest entity);
}

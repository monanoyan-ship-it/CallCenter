using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IPrivacyNoticeEntityService
{
    IQueryable<PrivacyNotice> GetAllQueryable();
    Task<PrivacyNotice?> GetByIdAsync(int id);
    Task<PrivacyNotice?> GetByUidAsync(Guid uid);
    void Add(PrivacyNotice entity);
    void Update(PrivacyNotice entity);
    void Remove(PrivacyNotice entity);
}

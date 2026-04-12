using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnConsentFormEntityService
{
    IQueryable<SlnConsentForm> GetAllQueryable();
    Task<SlnConsentForm?> GetByIdAsync(int id);
    void Add(SlnConsentForm entity);
    void Update(SlnConsentForm entity);
    void Remove(SlnConsentForm entity);
}

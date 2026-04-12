using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnClientConsentEntityService
{
    IQueryable<SlnClientConsent> GetAllQueryable();
    Task<SlnClientConsent?> GetByIdAsync(int id);
    void Add(SlnClientConsent entity);
    void Update(SlnClientConsent entity);
    void Remove(SlnClientConsent entity);
}

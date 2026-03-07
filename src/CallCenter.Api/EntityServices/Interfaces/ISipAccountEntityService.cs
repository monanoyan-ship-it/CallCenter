using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISipAccountEntityService
{
    IQueryable<SipAccount> GetAllQueryable();
    Task<SipAccount?> GetByIdAsync(int id);
    Task<SipAccount?> GetByIdWithCustomerAsync(int id);
    Task<SipAccount?> GetDefaultByCustomerAsync(int customerId);
    Task<SipAccount?> GetFirstActiveByCustomerAsync(int customerId);
    Task<SipAccount?> GetExistingDefaultAsync(int customerId, int? excludeId = null);
    Task<SipAccount?> GetByPersonnelAsync(int personnelId);
    Task<SipAccount?> GetFirstUnassignedAsync(int customerId);
    void Add(SipAccount entity);
    void Update(SipAccount entity);
}

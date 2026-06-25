using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISipLineEntityService
{
    IQueryable<SipLine> GetAllQueryable();
    Task<SipLine?> GetByIdAsync(int id);
    Task<SipLine?> GetByIdWithGatewayAsync(int id);
    Task<SipLine?> GetByPersonnelAsync(int personnelId);
    Task<SipLine?> AcquireUnassignedAsync(int customerId, int? excludeLineId = null);
    Task<SipLine?> AcquireUnassignedAsync(int customerId, int gatewayId, int? excludeLineId = null);

    /// <summary>
    /// Round-robin hat tahsisi: musterinin son tahsis edilen hattindan (Customer.LastAssignedSipLineId)
    /// SONRAKI bos hatti dondurur; sona gelince basa doner. Secilen hatti pointer'a yazar
    /// (kalici hale gelmesi icin cagiranin SaveChangesAsync cagirmasi gerekir).
    /// </summary>
    Task<SipLine?> AcquireNextRotatingAsync(int customerId, int? excludeLineId = null);

    Task ReleaseByPersonnelAsync(int personnelId);
    Task ReleaseStaleAllocationsAsync(TimeSpan maxAge);
    void Add(SipLine entity);
    void Update(SipLine entity);
}

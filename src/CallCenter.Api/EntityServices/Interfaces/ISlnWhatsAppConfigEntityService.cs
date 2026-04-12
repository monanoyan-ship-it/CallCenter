using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnWhatsAppConfigEntityService
{
    IQueryable<SlnWhatsAppConfig> GetAllQueryable();
    Task<SlnWhatsAppConfig?> GetByIdAsync(int id);
    void Add(SlnWhatsAppConfig entity);
    void Update(SlnWhatsAppConfig entity);
    void Remove(SlnWhatsAppConfig entity);
}

using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnWhatsAppMessageEntityService
{
    IQueryable<SlnWhatsAppMessage> GetAllQueryable();
    Task<SlnWhatsAppMessage?> GetByIdAsync(int id);
    void Add(SlnWhatsAppMessage entity);
    void Update(SlnWhatsAppMessage entity);
    void Remove(SlnWhatsAppMessage entity);
}

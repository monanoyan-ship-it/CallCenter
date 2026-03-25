using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnAppointmentEntityService
{
    IQueryable<SlnAppointment> GetAllQueryable();
    Task<SlnAppointment?> GetByIdAsync(int id);
    void Add(SlnAppointment entity);
    void Update(SlnAppointment entity);
    void Remove(SlnAppointment entity);
}

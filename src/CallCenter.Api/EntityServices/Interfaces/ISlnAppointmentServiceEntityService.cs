using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnAppointmentServiceEntityService
{
    IQueryable<SlnAppointmentService> GetAllQueryable();
    Task<SlnAppointmentService?> GetByIdAsync(int id);
    void Add(SlnAppointmentService entity);
    void Update(SlnAppointmentService entity);
    void Remove(SlnAppointmentService entity);
}

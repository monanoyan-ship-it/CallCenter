using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnAppointmentEntityService : ISlnAppointmentEntityService
{
    private readonly AppDbContext _db;

    public SlnAppointmentEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnAppointment> GetAllQueryable()
        => _db.SlnAppointments.AsQueryable();

    public Task<SlnAppointment?> GetByIdAsync(int id)
        => _db.SlnAppointments.FindAsync(id).AsTask();

    public void Add(SlnAppointment entity) => _db.SlnAppointments.Add(entity);
    public void Update(SlnAppointment entity) => _db.SlnAppointments.Update(entity);
    public void Remove(SlnAppointment entity) => _db.SlnAppointments.Remove(entity);
}

using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnAppointmentServiceEntityService : ISlnAppointmentServiceEntityService
{
    private readonly AppDbContext _db;

    public SlnAppointmentServiceEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnAppointmentService> GetAllQueryable()
        => _db.SlnAppointmentServices.AsQueryable();

    public Task<SlnAppointmentService?> GetByIdAsync(int id)
        => _db.SlnAppointmentServices.FindAsync(id).AsTask();

    public void Add(SlnAppointmentService entity) => _db.SlnAppointmentServices.Add(entity);
    public void Update(SlnAppointmentService entity) => _db.SlnAppointmentServices.Update(entity);
    public void Remove(SlnAppointmentService entity) => _db.SlnAppointmentServices.Remove(entity);
}

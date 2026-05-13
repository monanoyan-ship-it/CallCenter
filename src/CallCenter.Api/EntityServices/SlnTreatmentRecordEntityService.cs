using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnTreatmentRecordEntityService : ISlnTreatmentRecordEntityService
{
    private readonly AppDbContext _db;

    public SlnTreatmentRecordEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnTreatmentRecord> GetAllQueryable()
        => _db.SlnTreatmentRecords.AsQueryable();

    public Task<SlnTreatmentRecord?> GetByIdAsync(int id)
        => _db.SlnTreatmentRecords.FindAsync(id).AsTask();

    public void Add(SlnTreatmentRecord entity) => _db.SlnTreatmentRecords.Add(entity);
    public void Update(SlnTreatmentRecord entity) => _db.SlnTreatmentRecords.Update(entity);
    public void Remove(SlnTreatmentRecord entity) => _db.SlnTreatmentRecords.Remove(entity);
}

using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnTreatmentRecordEntityService
{
    IQueryable<SlnTreatmentRecord> GetAllQueryable();
    Task<SlnTreatmentRecord?> GetByIdAsync(int id);
    void Add(SlnTreatmentRecord entity);
    void Update(SlnTreatmentRecord entity);
    void Remove(SlnTreatmentRecord entity);
}

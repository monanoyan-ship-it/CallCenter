using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmSurveyResponseEntityService : ICrmSurveyResponseEntityService
{
    private readonly AppDbContext _db;

    public CrmSurveyResponseEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrmSurveyResponse> GetAllQueryable()
        => _db.CrmSurveyResponses.AsQueryable();

    public Task<CrmSurveyResponse?> GetByIdAsync(int id)
        => _db.CrmSurveyResponses.FindAsync(id).AsTask();

    public void Add(CrmSurveyResponse entity) => _db.CrmSurveyResponses.Add(entity);
    public void Remove(CrmSurveyResponse entity) => _db.CrmSurveyResponses.Remove(entity);
}

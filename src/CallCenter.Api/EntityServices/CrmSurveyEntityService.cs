using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmSurveyEntityService : ICrmSurveyEntityService
{
    private readonly AppDbContext _db;

    public CrmSurveyEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrmSurvey> GetAllQueryable()
        => _db.CrmSurveys.AsQueryable();

    public Task<CrmSurvey?> GetByIdAsync(int id)
        => _db.CrmSurveys.FindAsync(id).AsTask();

    public void Add(CrmSurvey entity) => _db.CrmSurveys.Add(entity);
    public void Update(CrmSurvey entity) => _db.CrmSurveys.Update(entity);
    public void Remove(CrmSurvey entity) => _db.CrmSurveys.Remove(entity);
}

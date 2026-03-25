using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmQualityAnswerSubCriteriaSelectionEntityService : ICrmQualityAnswerSubCriteriaSelectionEntityService
{
    private readonly AppDbContext _db;

    public CrmQualityAnswerSubCriteriaSelectionEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrmQualityAnswerSubCriteriaSelection> GetAllQueryable()
        => _db.CrmQualityAnswerSubCriteriaSelections.AsQueryable();

    public void AddRange(IEnumerable<CrmQualityAnswerSubCriteriaSelection> entities)
        => _db.CrmQualityAnswerSubCriteriaSelections.AddRange(entities);

    public void DeleteRange(IEnumerable<CrmQualityAnswerSubCriteriaSelection> entities)
        => _db.CrmQualityAnswerSubCriteriaSelections.RemoveRange(entities);
}

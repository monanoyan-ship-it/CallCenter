using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class QualityAnswerSubCriteriaSelectionEntityService : IQualityAnswerSubCriteriaSelectionEntityService
{
    private readonly AppDbContext _db;

    public QualityAnswerSubCriteriaSelectionEntityService(AppDbContext db) => _db = db;

    public IQueryable<QualityAnswerSubCriteriaSelection> GetAllQueryable()
        => _db.QualityAnswerSubCriteriaSelections.AsQueryable();

    public void AddRange(IEnumerable<QualityAnswerSubCriteriaSelection> entities)
        => _db.QualityAnswerSubCriteriaSelections.AddRange(entities);

    public void DeleteRange(IEnumerable<QualityAnswerSubCriteriaSelection> entities)
        => _db.QualityAnswerSubCriteriaSelections.RemoveRange(entities);
}

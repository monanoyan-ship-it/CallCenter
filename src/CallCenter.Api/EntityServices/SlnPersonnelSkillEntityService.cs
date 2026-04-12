using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnPersonnelSkillEntityService : ISlnPersonnelSkillEntityService
{
    private readonly AppDbContext _db;

    public SlnPersonnelSkillEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnPersonnelSkill> GetAllQueryable()
        => _db.SlnPersonnelSkills.AsQueryable();

    public void Add(SlnPersonnelSkill entity) => _db.SlnPersonnelSkills.Add(entity);
    public void Remove(SlnPersonnelSkill entity) => _db.SlnPersonnelSkills.Remove(entity);
}

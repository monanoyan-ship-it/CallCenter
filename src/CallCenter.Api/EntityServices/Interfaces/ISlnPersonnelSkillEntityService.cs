using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnPersonnelSkillEntityService
{
    IQueryable<SlnPersonnelSkill> GetAllQueryable();
    void Add(SlnPersonnelSkill entity);
    void Remove(SlnPersonnelSkill entity);
}

using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IDataDestructionLogEntityService
{
    IQueryable<DataDestructionLog> GetAllQueryable();
    void Add(DataDestructionLog entity);
}

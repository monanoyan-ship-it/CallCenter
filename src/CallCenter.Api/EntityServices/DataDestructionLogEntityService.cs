using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class DataDestructionLogEntityService : IDataDestructionLogEntityService
{
    private readonly AppDbContext _db;

    public DataDestructionLogEntityService(AppDbContext db) => _db = db;

    public IQueryable<DataDestructionLog> GetAllQueryable() => _db.DataDestructionLogs.AsQueryable();
    public void Add(DataDestructionLog entity) => _db.DataDestructionLogs.Add(entity);
}

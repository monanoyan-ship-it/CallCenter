using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IMonitoringSessionEntityService
{
    IQueryable<CallMonitoringSession> GetAllQueryable();
    Task<CallMonitoringSession?> GetByIdAsync(int id);
    void Add(CallMonitoringSession entity);
}

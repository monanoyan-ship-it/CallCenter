using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IOrphanRecordingEntityService
{
    Task<OrphanRecording?> GetByUidAsync(Guid uid);
    Task<OrphanRecording?> GetByCloudFileIdAsync(int customerId, string cloudFileId);
    IQueryable<OrphanRecording> GetAllQueryable();
    void Add(OrphanRecording entity);
    void Update(OrphanRecording entity);
}

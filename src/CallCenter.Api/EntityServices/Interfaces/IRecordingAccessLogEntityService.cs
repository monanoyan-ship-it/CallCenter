using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IRecordingAccessLogEntityService
{
    void Add(RecordingAccessLog entity);
    Task<List<RecordingAccessLog>> GetByCallRecordIdAsync(int callRecordId);
}

using CallCenter.Windows.LocalData.Entities;

namespace CallCenter.Windows.LocalData.Providers;

/// <summary>
/// Lokal DB ayarlanmamissa kullanilan bos repository.
/// Hicbir sey yapmaz, hata vermez — uygulama DB olmadan da calisir.
/// (Graceful degradation: sadece backend'e yazar, lokal kayit tutmaz)
/// </summary>
public class NullLocalRepository : ILocalRepository
{
    public bool IsConfigured => false;

    public Task<bool> TestConnectionAsync() => Task.FromResult(false);
    public Task InitializeAsync() => Task.CompletedTask;

    public Task SaveCallRecordAsync(LocalCallRecord record) => Task.CompletedTask;
    public Task UpdateCallRecordAsync(LocalCallRecord record) => Task.CompletedTask;
    public Task<LocalCallRecord?> GetCallRecordByUidAsync(Guid uid) => Task.FromResult<LocalCallRecord?>(null);
    public Task<List<LocalCallRecord>> GetCallRecordsAsync(DateTime? from, DateTime? to, int page, int pageSize) => Task.FromResult(new List<LocalCallRecord>());
    public Task<List<LocalCallRecord>> GetUnsyncedRecordsAsync(int limit = 50) => Task.FromResult(new List<LocalCallRecord>());
    public Task MarkAsSyncedAsync(Guid uid, int? backendCallId = null) => Task.CompletedTask;

    public Task SaveRecordingMetadataAsync(LocalRecording recording) => Task.CompletedTask;
    public Task<List<LocalRecording>> GetRecordingsAsync(Guid? callRecordUid, int page, int pageSize) => Task.FromResult(new List<LocalRecording>());

    public Task<LocalCallStats> GetStatsAsync(DateTime from, DateTime to) => Task.FromResult(new LocalCallStats());
}

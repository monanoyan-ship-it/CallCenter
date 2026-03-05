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
    public Task DeleteCallRecordAsync(Guid uid) => Task.CompletedTask;

    public Task SaveRecordingMetadataAsync(LocalRecording recording) => Task.CompletedTask;
    public Task<List<LocalRecording>> GetRecordingsAsync(Guid? callRecordUid, int page, int pageSize) => Task.FromResult(new List<LocalRecording>());
    public Task<List<LocalRecording>> GetExpiredRecordingsAsync() => Task.FromResult(new List<LocalRecording>());
    public Task DeleteRecordingAsync(Guid uid) => Task.CompletedTask;
    public Task<List<LocalRecording>> GetUnuploadedRecordingsAsync(int limit = 10) => Task.FromResult(new List<LocalRecording>());
    public Task MarkRecordingAsUploadedAsync(Guid uid, string? cloudFileId = null) => Task.CompletedTask;
    public Task MarkRecordingAsUploadedToPlatformAsync(Guid uid, string? platformFileId = null) => Task.CompletedTask;
    public Task UpdateRecordingUploadAttemptAsync(Guid uid) => Task.CompletedTask;
    public Task UpdateRecordingPlatformUploadAttemptAsync(Guid uid) => Task.CompletedTask;

    public Task<LocalCallStats> GetStatsAsync(DateTime from, DateTime to) => Task.FromResult(new LocalCallStats());

    // SIP HESAPLARI — STUB METODLAR (DB yok, yapmiyor)
    public Task SaveSipAccountAsync(LocalSipAccount account) => Task.CompletedTask;
    public Task UpdateSipAccountAsync(LocalSipAccount account) => Task.CompletedTask;
    public Task<LocalSipAccount?> GetSipAccountByUidAsync(Guid uid) => Task.FromResult<LocalSipAccount?>(null);
    public Task<LocalSipAccount?> GetSipAccountByIdAsync(int id) => Task.FromResult<LocalSipAccount?>(null);
    public Task<List<LocalSipAccount>> GetAllSipAccountsAsync(int page = 1, int pageSize = 50) => Task.FromResult(new List<LocalSipAccount>());
    public Task<LocalSipAccount?> GetDefaultSipAccountAsync() => Task.FromResult<LocalSipAccount?>(null);
    public Task DeleteSipAccountAsync(int id) => Task.CompletedTask;
    public Task<List<LocalSipAccount>> GetUnsyncedSipAccountsAsync(int limit = 50) => Task.FromResult(new List<LocalSipAccount>());
    public Task MarkSipAccountAsSyncedAsync(Guid uid, int? backendSipAccountId = null) => Task.CompletedTask;

    // CONTACT BUFFER — STUB METODLAR
    public Task SaveContactAsync(LocalContact contact) => Task.CompletedTask;
    public Task<List<LocalContact>> GetUnsyncedContactsAsync(int limit = 50) => Task.FromResult(new List<LocalContact>());
    public Task DeleteContactAsync(Guid uid) => Task.CompletedTask;
}

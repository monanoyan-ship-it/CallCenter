using System.IO;
using CallCenter.Windows.LocalData.Entities;

namespace CallCenter.Windows.LocalData.Providers;

/// <summary>
/// Dosya tabanli ILocalRepository implementasyonu.
/// %LOCALAPPDATA%\CallCenter\Data\ altinda JSON dosyalarinda veri saklar.
/// Mevcut ContactService/SecureStorage ile ayni pattern.
/// </summary>
public class FileLocalRepository : ILocalRepository
{
    private readonly LocalFileStore<LocalCallRecord> _callRecords;
    private readonly LocalFileStore<LocalSipAccount> _sipAccounts;
    private readonly LocalFileStore<LocalRecording> _recordings;
    private readonly string _basePath;

    public FileLocalRepository(string basePath)
    {
        _basePath = basePath;
        _callRecords = new LocalFileStore<LocalCallRecord>(basePath, "call-records.json");
        _sipAccounts = new LocalFileStore<LocalSipAccount>(basePath, "sip-accounts.json");
        _recordings = new LocalFileStore<LocalRecording>(basePath, "recordings.json");
    }

    // ═══════════════════════════════════════
    // BAGLANTI
    // ═══════════════════════════════════════

    public bool IsConfigured => true;

    public Task<bool> TestConnectionAsync()
    {
        try
        {
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);

            // Yazma testi: gecici dosya olustur ve sil
            var testFile = Path.Combine(_basePath, ".write-test");
            File.WriteAllText(testFile, "ok");
            File.Delete(testFile);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task InitializeAsync()
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
        return Task.CompletedTask;
    }

    // ═══════════════════════════════════════
    // CAGRI KAYITLARI
    // ═══════════════════════════════════════

    public Task SaveCallRecordAsync(LocalCallRecord record)
        => _callRecords.AddAsync(record);

    public async Task UpdateCallRecordAsync(LocalCallRecord record)
    {
        var updated = await _callRecords.UpdateAsync(
            r => r.Uid == record.Uid,
            r =>
            {
                r.CallerNumber = record.CallerNumber;
                r.CalleeNumber = record.CalleeNumber;
                r.Direction = record.Direction;
                r.Status = record.Status;
                r.StartedAt = record.StartedAt;
                r.AnsweredAt = record.AnsweredAt;
                r.EndedAt = record.EndedAt;
                r.DurationSeconds = record.DurationSeconds;
                r.AgentName = record.AgentName;
                r.QueueName = record.QueueName;
                r.Notes = record.Notes;
                r.RecordingFilePath = record.RecordingFilePath;
                r.IsSyncedToBackend = record.IsSyncedToBackend;
                r.LastSyncAttempt = record.LastSyncAttempt;
                r.BackendCallId = record.BackendCallId;
            });

        // Bulunamadiysa yeni kayit olarak ekle
        if (!updated)
            await _callRecords.AddAsync(record);
    }

    public Task<LocalCallRecord?> GetCallRecordByUidAsync(Guid uid)
        => _callRecords.FindAsync(r => r.Uid == uid);

    public async Task DeleteCallRecordAsync(Guid uid)
    {
        await _callRecords.RemoveAsync(r => r.Uid == uid);
        // Iliskili ses kaydi metadata'sini da temizle
        await _recordings.RemoveAllAsync(r => r.CallRecordUid == uid);
    }

    public async Task<List<LocalCallRecord>> GetCallRecordsAsync(DateTime? from, DateTime? to, int page, int pageSize)
    {
        var all = await _callRecords.WhereAsync(r =>
            (!from.HasValue || r.StartedAt >= from.Value) &&
            (!to.HasValue || r.StartedAt <= to.Value));

        return all
            .OrderByDescending(r => r.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public Task<List<LocalCallRecord>> GetUnsyncedRecordsAsync(int limit = 50)
        => _callRecords.WhereAsync(r => !r.IsSyncedToBackend);

    public async Task MarkAsSyncedAsync(Guid uid, int? backendCallId = null)
    {
        await _callRecords.UpdateAsync(
            r => r.Uid == uid,
            r =>
            {
                r.IsSyncedToBackend = true;
                r.LastSyncAttempt = DateTime.UtcNow;
                if (backendCallId.HasValue)
                    r.BackendCallId = backendCallId.Value;
            });
    }

    // ═══════════════════════════════════════
    // SES KAYITLARI
    // ═══════════════════════════════════════

    public Task SaveRecordingMetadataAsync(LocalRecording recording)
        => _recordings.AddAsync(recording);

    public async Task<List<LocalRecording>> GetRecordingsAsync(Guid? callRecordUid, int page, int pageSize)
    {
        var all = callRecordUid.HasValue
            ? await _recordings.WhereAsync(r => r.CallRecordUid == callRecordUid.Value)
            : await _recordings.GetAllAsync();

        return all
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public Task<List<LocalRecording>> GetExpiredRecordingsAsync()
        => _recordings.WhereAsync(r => r.RetentionDate.HasValue && r.RetentionDate.Value < DateTime.UtcNow);

    public async Task DeleteRecordingAsync(Guid uid)
        => await _recordings.RemoveAsync(r => r.Uid == uid);

    // ═══════════════════════════════════════
    // ISTATISTIKLER
    // ═══════════════════════════════════════

    public async Task<LocalCallStats> GetStatsAsync(DateTime from, DateTime to)
    {
        var records = await _callRecords.WhereAsync(r =>
            r.StartedAt >= from && r.StartedAt <= to);

        return new LocalCallStats
        {
            TotalCalls = records.Count,
            AnsweredCalls = records.Count(r => r.Status == "Completed"),
            MissedCalls = records.Count(r => r.Status == "Missed"),
            TotalDurationSeconds = records
                .Where(r => r.Status == "Completed")
                .Sum(r => r.DurationSeconds)
        };
    }

    // ═══════════════════════════════════════
    // SIP HESAPLARI (GATEWAY)
    // ═══════════════════════════════════════

    public async Task SaveSipAccountAsync(LocalSipAccount account)
    {
        // Auto-increment ID ata
        var all = await _sipAccounts.GetAllAsync();
        account.Id = all.Count > 0 ? all.Max(a => a.Id) + 1 : 1;
        await _sipAccounts.AddAsync(account);
    }

    public async Task UpdateSipAccountAsync(LocalSipAccount account)
    {
        var updated = await _sipAccounts.UpdateAsync(
            a => a.Id == account.Id,
            a =>
            {
                a.Uid = account.Uid;
                a.Name = account.Name;
                a.Server = account.Server;
                a.Port = account.Port;
                a.Domain = account.Domain;
                a.Username = account.Username;
                a.Password = account.Password;
                a.Transport = account.Transport;
                a.WsUri = account.WsUri;
                a.UseSrtp = account.UseSrtp;
                a.StunServer = account.StunServer;
                a.TurnServer = account.TurnServer;
                a.TurnUsername = account.TurnUsername;
                a.TurnPassword = account.TurnPassword;
                a.PreferredCodecs = account.PreferredCodecs;
                a.JitterBufferMinMs = account.JitterBufferMinMs;
                a.JitterBufferMaxMs = account.JitterBufferMaxMs;
                a.IsDefault = account.IsDefault;
                a.IsActive = account.IsActive;
                a.IsSyncedToBackend = account.IsSyncedToBackend;
                a.LastSyncAttempt = account.LastSyncAttempt;
                a.BackendSipAccountId = account.BackendSipAccountId;
            });

        if (!updated)
            await _sipAccounts.AddAsync(account);
    }

    public Task<LocalSipAccount?> GetSipAccountByUidAsync(Guid uid)
        => _sipAccounts.FindAsync(a => a.Uid == uid);

    public Task<LocalSipAccount?> GetSipAccountByIdAsync(int id)
        => _sipAccounts.FindAsync(a => a.Id == id);

    public async Task<List<LocalSipAccount>> GetAllSipAccountsAsync(int page = 1, int pageSize = 50)
    {
        var all = await _sipAccounts.GetAllAsync();
        return all
            .OrderBy(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public Task<LocalSipAccount?> GetDefaultSipAccountAsync()
        => _sipAccounts.FindAsync(a => a.IsDefault && a.IsActive);

    public async Task DeleteSipAccountAsync(int id)
        => await _sipAccounts.RemoveAsync(a => a.Id == id);

    public Task<List<LocalSipAccount>> GetUnsyncedSipAccountsAsync(int limit = 50)
        => _sipAccounts.WhereAsync(a => !a.IsSyncedToBackend);

    public async Task MarkSipAccountAsSyncedAsync(Guid uid, int? backendSipAccountId = null)
    {
        await _sipAccounts.UpdateAsync(
            a => a.Uid == uid,
            a =>
            {
                a.IsSyncedToBackend = true;
                a.LastSyncAttempt = DateTime.UtcNow;
                if (backendSipAccountId.HasValue)
                    a.BackendSipAccountId = backendSipAccountId.Value;
            });
    }

    // ═══════════════════════════════════════
    // YARDIMCI (Settings sayfasi icin)
    // ═══════════════════════════════════════

    /// <summary>Veri klasoru yolu</summary>
    public string BasePath => _basePath;

    /// <summary>Her store'un dosya boyutunu dondurur</summary>
    public Dictionary<string, long> GetFileSizes() => new()
    {
        { "call-records.json", _callRecords.GetFileSize() },
        { "sip-accounts.json", _sipAccounts.GetFileSize() },
        { "recordings.json", _recordings.GetFileSize() }
    };

    /// <summary>Tum verileri temizle</summary>
    public async Task ClearAllAsync()
    {
        await _callRecords.ClearAsync();
        await _sipAccounts.ClearAsync();
        await _recordings.ClearAsync();
    }
}

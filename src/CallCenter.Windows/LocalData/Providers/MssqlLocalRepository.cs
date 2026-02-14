using CallCenter.Windows.LocalData.Entities;
using Microsoft.Data.SqlClient;

namespace CallCenter.Windows.LocalData.Providers;

/// <summary>
/// Microsoft SQL Server tabanli lokal repository.
/// Microsoft.Data.SqlClient ile dogrudan T-SQL calistirir.
///
/// Kullanim:
///   var repo = new MssqlLocalRepository("Server=localhost;Database=callcenter_local;...");
///   await repo.InitializeAsync();   // tablolari olustur
///   await repo.SaveCallRecordAsync(record);
/// </summary>
public class MssqlLocalRepository : ILocalRepository
{
    private readonly string _connectionString;

    public MssqlLocalRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    // ═══════════════════════════════════════
    // BAGLANTI
    // ═══════════════════════════════════════

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// T-SQL ile tablolari olusturur (yoksa).
    /// MSSQL'de "IF NOT EXISTS" kontrolu sys.tables uzerinden yapilir.
    /// </summary>
    public async Task InitializeAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // ── Cagri kayitlari tablosu ──
        await using var cmdCalls = new SqlCommand("""
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'local_call_records')
            BEGIN
                CREATE TABLE local_call_records (
                    uid              UNIQUEIDENTIFIER PRIMARY KEY,
                    caller_number    NVARCHAR(100) NOT NULL DEFAULT '',
                    callee_number    NVARCHAR(100) NOT NULL DEFAULT '',
                    direction        NVARCHAR(20)  NOT NULL DEFAULT 'Outbound',
                    status           NVARCHAR(20)  NOT NULL DEFAULT 'Ringing',
                    started_at       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
                    answered_at      DATETIME2     NULL,
                    ended_at         DATETIME2     NULL,
                    duration_seconds INT           NOT NULL DEFAULT 0,
                    agent_name       NVARCHAR(200) NULL,
                    queue_name       NVARCHAR(200) NULL,
                    notes            NVARCHAR(MAX) NULL,
                    recording_file_path NVARCHAR(500) NULL,
                    is_synced        BIT           NOT NULL DEFAULT 0,
                    last_sync_attempt DATETIME2    NULL,
                    backend_call_id  INT           NULL
                );

                CREATE INDEX idx_call_records_started_at ON local_call_records(started_at);
                CREATE INDEX idx_call_records_is_synced ON local_call_records(is_synced);
            END
            """, conn);
        await cmdCalls.ExecuteNonQueryAsync();

        // ── Ses kayitlari tablosu ──
        await using var cmdRecs = new SqlCommand("""
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'local_recordings')
            BEGIN
                CREATE TABLE local_recordings (
                    uid              UNIQUEIDENTIFIER PRIMARY KEY,
                    call_record_uid  UNIQUEIDENTIFIER NOT NULL,
                    file_path        NVARCHAR(500) NOT NULL DEFAULT '',
                    file_size        BIGINT        NOT NULL DEFAULT 0,
                    format           NVARCHAR(10)  NOT NULL DEFAULT 'wav',
                    duration_seconds INT           NOT NULL DEFAULT 0,
                    created_at       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
                    file_hash        NVARCHAR(128) NULL,
                    is_encrypted     BIT           NOT NULL DEFAULT 0,
                    retention_date   DATETIME2     NULL
                );

                CREATE INDEX idx_recordings_call_uid ON local_recordings(call_record_uid);
                CREATE INDEX idx_recordings_retention ON local_recordings(retention_date) WHERE retention_date IS NOT NULL;
            END
            """, conn);
        await cmdRecs.ExecuteNonQueryAsync();
    }

    // ═══════════════════════════════════════
    // CAGRI KAYITLARI — CRUD
    // ═══════════════════════════════════════

    public async Task SaveCallRecordAsync(LocalCallRecord record)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("""
            INSERT INTO local_call_records
                (uid, caller_number, callee_number, direction, status,
                 started_at, answered_at, ended_at, duration_seconds,
                 agent_name, queue_name, notes, recording_file_path,
                 is_synced, last_sync_attempt, backend_call_id)
            VALUES
                (@uid, @caller, @callee, @dir, @status,
                 @started, @answered, @ended, @duration,
                 @agent, @queue, @notes, @recPath,
                 @synced, @lastSync, @backendId)
            """, conn);

        AddCallRecordParams(cmd, record);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateCallRecordAsync(LocalCallRecord record)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("""
            UPDATE local_call_records SET
                caller_number = @caller,
                callee_number = @callee,
                direction = @dir,
                status = @status,
                started_at = @started,
                answered_at = @answered,
                ended_at = @ended,
                duration_seconds = @duration,
                agent_name = @agent,
                queue_name = @queue,
                notes = @notes,
                recording_file_path = @recPath,
                is_synced = @synced,
                last_sync_attempt = @lastSync,
                backend_call_id = @backendId
            WHERE uid = @uid
            """, conn);

        AddCallRecordParams(cmd, record);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<LocalCallRecord?> GetCallRecordByUidAsync(Guid uid)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(
            "SELECT * FROM local_call_records WHERE uid = @uid", conn);
        cmd.Parameters.AddWithValue("@uid", uid);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapCallRecord(reader);
    }

    /// <summary>
    /// Tarih araliginda sayfalanmis kayitlar.
    /// MSSQL'de OFFSET-FETCH kullanilir (SQL Server 2012+).
    /// </summary>
    public async Task<List<LocalCallRecord>> GetCallRecordsAsync(
        DateTime? from, DateTime? to, int page, int pageSize)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var where = "WHERE 1=1";
        if (from.HasValue) where += " AND started_at >= @from";
        if (to.HasValue) where += " AND started_at <= @to";

        var sql = $"""
            SELECT * FROM local_call_records
            {where}
            ORDER BY started_at DESC
            OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY
            """;

        await using var cmd = new SqlCommand(sql, conn);
        if (from.HasValue) cmd.Parameters.AddWithValue("@from", from.Value);
        if (to.HasValue) cmd.Parameters.AddWithValue("@to", to.Value);
        cmd.Parameters.AddWithValue("@limit", pageSize);
        cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

        return await ReadCallRecordListAsync(cmd);
    }

    public async Task<List<LocalCallRecord>> GetUnsyncedRecordsAsync(int limit = 50)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("""
            SELECT TOP(@limit) * FROM local_call_records
            WHERE is_synced = 0
            ORDER BY started_at ASC
            """, conn);
        cmd.Parameters.AddWithValue("@limit", limit);

        return await ReadCallRecordListAsync(cmd);
    }

    public async Task MarkAsSyncedAsync(Guid uid, int? backendCallId = null)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("""
            UPDATE local_call_records SET
                is_synced = 1,
                last_sync_attempt = @now,
                backend_call_id = @backendId
            WHERE uid = @uid
            """, conn);
        cmd.Parameters.AddWithValue("@uid", uid);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@backendId", (object?)backendCallId ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    // ═══════════════════════════════════════
    // SES KAYITLARI
    // ═══════════════════════════════════════

    public async Task SaveRecordingMetadataAsync(LocalRecording recording)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("""
            INSERT INTO local_recordings
                (uid, call_record_uid, file_path, file_size, format, duration_seconds, created_at,
                 file_hash, is_encrypted, retention_date)
            VALUES
                (@uid, @callUid, @path, @size, @format, @duration, @created,
                 @fileHash, @encrypted, @retention)
            """, conn);

        cmd.Parameters.AddWithValue("@uid", recording.Uid);
        cmd.Parameters.AddWithValue("@callUid", recording.CallRecordUid);
        cmd.Parameters.AddWithValue("@path", recording.FilePath);
        cmd.Parameters.AddWithValue("@size", recording.FileSize);
        cmd.Parameters.AddWithValue("@format", recording.Format);
        cmd.Parameters.AddWithValue("@duration", recording.DurationSeconds);
        cmd.Parameters.AddWithValue("@created", recording.CreatedAt);
        cmd.Parameters.AddWithValue("@fileHash", (object?)recording.FileHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@encrypted", recording.IsEncrypted);
        cmd.Parameters.AddWithValue("@retention", (object?)recording.RetentionDate ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<LocalRecording>> GetRecordingsAsync(
        Guid? callRecordUid, int page, int pageSize)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var where = callRecordUid.HasValue ? "WHERE call_record_uid = @callUid" : "";
        var sql = $"""
            SELECT * FROM local_recordings
            {where}
            ORDER BY created_at DESC
            OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY
            """;

        await using var cmd = new SqlCommand(sql, conn);
        if (callRecordUid.HasValue) cmd.Parameters.AddWithValue("@callUid", callRecordUid.Value);
        cmd.Parameters.AddWithValue("@limit", pageSize);
        cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

        var list = new List<LocalRecording>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapRecording(reader));
        }
        return list;
    }

    /// <summary>
    /// Saklama suresi dolmus ses kayitlarini getirir.
    /// </summary>
    public async Task<List<LocalRecording>> GetExpiredRecordingsAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("""
            SELECT * FROM local_recordings
            WHERE retention_date IS NOT NULL AND retention_date < @now
            """, conn);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);

        var list = new List<LocalRecording>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapRecording(reader));
        }
        return list;
    }

    /// <summary>
    /// Ses kaydi metadata'sini siler.
    /// </summary>
    public async Task DeleteRecordingAsync(Guid uid)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(
            "DELETE FROM local_recordings WHERE uid = @uid", conn);
        cmd.Parameters.AddWithValue("@uid", uid);
        await cmd.ExecuteNonQueryAsync();
    }

    // ═══════════════════════════════════════
    // ISTATISTIKLER
    // ═══════════════════════════════════════

    /// <summary>
    /// MSSQL istatistik sorgusu.
    /// PostgreSQL'deki FILTER (WHERE ...) yerine CASE WHEN kullanilir.
    /// </summary>
    public async Task<LocalCallStats> GetStatsAsync(DateTime from, DateTime to)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("""
            SELECT
                COUNT(*) AS total,
                SUM(CASE WHEN status = 'Completed' THEN 1 ELSE 0 END) AS answered,
                SUM(CASE WHEN status = 'Missed' THEN 1 ELSE 0 END) AS missed,
                ISNULL(SUM(CASE WHEN status = 'Completed' THEN duration_seconds ELSE 0 END), 0) AS total_duration
            FROM local_call_records
            WHERE started_at >= @from AND started_at <= @to
            """, conn);
        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return new LocalCallStats();

        return new LocalCallStats
        {
            TotalCalls = reader.GetInt32(reader.GetOrdinal("total")),
            AnsweredCalls = reader.GetInt32(reader.GetOrdinal("answered")),
            MissedCalls = reader.GetInt32(reader.GetOrdinal("missed")),
            TotalDurationSeconds = reader.GetInt32(reader.GetOrdinal("total_duration"))
        };
    }

    // ═══════════════════════════════════════
    // YARDIMCI METODLAR
    // ═══════════════════════════════════════

    /// <summary>
    /// MSSQL parametreleri "@" on-eki ile eklenir (Npgsql'den farki).
    /// </summary>
    private static void AddCallRecordParams(SqlCommand cmd, LocalCallRecord r)
    {
        cmd.Parameters.AddWithValue("@uid", r.Uid);
        cmd.Parameters.AddWithValue("@caller", r.CallerNumber);
        cmd.Parameters.AddWithValue("@callee", r.CalleeNumber);
        cmd.Parameters.AddWithValue("@dir", r.Direction);
        cmd.Parameters.AddWithValue("@status", r.Status);
        cmd.Parameters.AddWithValue("@started", r.StartedAt);
        cmd.Parameters.AddWithValue("@answered", (object?)r.AnsweredAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ended", (object?)r.EndedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@duration", r.DurationSeconds);
        cmd.Parameters.AddWithValue("@agent", (object?)r.AgentName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@queue", (object?)r.QueueName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@notes", (object?)r.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@recPath", (object?)r.RecordingFilePath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@synced", r.IsSyncedToBackend);
        cmd.Parameters.AddWithValue("@lastSync", (object?)r.LastSyncAttempt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@backendId", (object?)r.BackendCallId ?? DBNull.Value);
    }

    private static LocalCallRecord MapCallRecord(SqlDataReader reader)
    {
        return new LocalCallRecord
        {
            Uid = reader.GetGuid(reader.GetOrdinal("uid")),
            CallerNumber = reader.GetString(reader.GetOrdinal("caller_number")),
            CalleeNumber = reader.GetString(reader.GetOrdinal("callee_number")),
            Direction = reader.GetString(reader.GetOrdinal("direction")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            StartedAt = reader.GetDateTime(reader.GetOrdinal("started_at")),
            AnsweredAt = reader.IsDBNull(reader.GetOrdinal("answered_at"))
                ? null : reader.GetDateTime(reader.GetOrdinal("answered_at")),
            EndedAt = reader.IsDBNull(reader.GetOrdinal("ended_at"))
                ? null : reader.GetDateTime(reader.GetOrdinal("ended_at")),
            DurationSeconds = reader.GetInt32(reader.GetOrdinal("duration_seconds")),
            AgentName = reader.IsDBNull(reader.GetOrdinal("agent_name"))
                ? null : reader.GetString(reader.GetOrdinal("agent_name")),
            QueueName = reader.IsDBNull(reader.GetOrdinal("queue_name"))
                ? null : reader.GetString(reader.GetOrdinal("queue_name")),
            Notes = reader.IsDBNull(reader.GetOrdinal("notes"))
                ? null : reader.GetString(reader.GetOrdinal("notes")),
            RecordingFilePath = reader.IsDBNull(reader.GetOrdinal("recording_file_path"))
                ? null : reader.GetString(reader.GetOrdinal("recording_file_path")),
            IsSyncedToBackend = reader.GetBoolean(reader.GetOrdinal("is_synced")),
            LastSyncAttempt = reader.IsDBNull(reader.GetOrdinal("last_sync_attempt"))
                ? null : reader.GetDateTime(reader.GetOrdinal("last_sync_attempt")),
            BackendCallId = reader.IsDBNull(reader.GetOrdinal("backend_call_id"))
                ? null : reader.GetInt32(reader.GetOrdinal("backend_call_id"))
        };
    }

    private static LocalRecording MapRecording(SqlDataReader reader)
    {
        var rec = new LocalRecording
        {
            Uid = reader.GetGuid(reader.GetOrdinal("uid")),
            CallRecordUid = reader.GetGuid(reader.GetOrdinal("call_record_uid")),
            FilePath = reader.GetString(reader.GetOrdinal("file_path")),
            FileSize = reader.GetInt64(reader.GetOrdinal("file_size")),
            Format = reader.GetString(reader.GetOrdinal("format")),
            DurationSeconds = reader.GetInt32(reader.GetOrdinal("duration_seconds")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };

        var ordFileHash = TryGetOrdinal(reader, "file_hash");
        if (ordFileHash >= 0 && !reader.IsDBNull(ordFileHash))
            rec.FileHash = reader.GetString(ordFileHash);

        var ordEncrypted = TryGetOrdinal(reader, "is_encrypted");
        if (ordEncrypted >= 0)
            rec.IsEncrypted = reader.GetBoolean(ordEncrypted);

        var ordRetention = TryGetOrdinal(reader, "retention_date");
        if (ordRetention >= 0 && !reader.IsDBNull(ordRetention))
            rec.RetentionDate = reader.GetDateTime(ordRetention);

        return rec;
    }

    private static int TryGetOrdinal(SqlDataReader reader, string name)
    {
        try { return reader.GetOrdinal(name); }
        catch { return -1; }
    }

    private static async Task<List<LocalCallRecord>> ReadCallRecordListAsync(SqlCommand cmd)
    {
        var list = new List<LocalCallRecord>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapCallRecord(reader));
        }
        return list;
    }
}

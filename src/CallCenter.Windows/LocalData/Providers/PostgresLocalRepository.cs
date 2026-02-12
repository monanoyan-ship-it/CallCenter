using CallCenter.Windows.LocalData.Entities;
using Npgsql;

namespace CallCenter.Windows.LocalData.Providers;

/// <summary>
/// PostgreSQL tabanli lokal repository.
/// Npgsql kutuphanesi ile dogrudan SQL calistirir (EF Core kullanmaz — hafif tutar).
///
/// Kullanim:
///   var repo = new PostgresLocalRepository("Host=localhost;Database=callcenter_local;...");
///   await repo.InitializeAsync();   // tablolari olustur
///   await repo.SaveCallRecordAsync(record);
/// </summary>
public class PostgresLocalRepository : ILocalRepository
{
    private readonly string _connectionString;

    public PostgresLocalRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    // ═══════════════════════════════════════
    // BAGLANTI
    // ═══════════════════════════════════════

    /// <summary>
    /// PostgreSQL sunucusuna baglanabilir mi?
    /// Hedef database henuz yoksa postgres default DB'sine baglanarak sunucuyu test eder.
    /// Ayarlar ekranindaki "Baglanti Test Et" butonu bunu cagirir.
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // Once hedef DB'ye baglanmayi dene
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync();
            return true;
        }
        catch (NpgsqlException ex) when (ex.SqlState == "3D000") // database does not exist
        {
            // Database yok ama sunucu erisim var mi? postgres DB'sine baglan
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(_connectionString);
                builder.Database = "postgres";
                await using var conn = new NpgsqlConnection(builder.ToString());
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT 1", conn);
                await cmd.ExecuteScalarAsync();
                return true; // sunucu erisilebilir, DB "Tablolari Olustur" ile yaratilacak
            }
            catch
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Once database'i olusturur (yoksa), sonra tablolari olusturur.
    /// Migration sistemi yok — CREATE DATABASE + CREATE TABLE IF NOT EXISTS ile calısır.
    /// Ayarlar ekranındaki "Tablolari Olustur" butonu bunu cagirir.
    /// </summary>
    public async Task InitializeAsync()
    {
        // ── 1) Database yoksa olustur ──
        await EnsureDatabaseExistsAsync();

        // ── 2) Tablolari olustur ──
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // ── Cagri kayitlari tablosu ──
        await using var cmdCalls = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS local_call_records (
                uid              UUID PRIMARY KEY,
                caller_number    TEXT NOT NULL DEFAULT '',
                callee_number    TEXT NOT NULL DEFAULT '',
                direction        TEXT NOT NULL DEFAULT 'Outbound',
                status           TEXT NOT NULL DEFAULT 'Ringing',
                started_at       TIMESTAMP NOT NULL DEFAULT NOW(),
                answered_at      TIMESTAMP,
                ended_at         TIMESTAMP,
                duration_seconds INTEGER NOT NULL DEFAULT 0,
                agent_name       TEXT,
                queue_name       TEXT,
                notes            TEXT,
                recording_file_path TEXT,
                is_synced        BOOLEAN NOT NULL DEFAULT FALSE,
                last_sync_attempt TIMESTAMP,
                backend_call_id  INTEGER
            );

            CREATE INDEX IF NOT EXISTS idx_call_records_started_at ON local_call_records(started_at);
            CREATE INDEX IF NOT EXISTS idx_call_records_is_synced ON local_call_records(is_synced);
            """, conn);
        await cmdCalls.ExecuteNonQueryAsync();

        // ── Ses kayitlari tablosu ──
        await using var cmdRecs = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS local_recordings (
                uid              UUID PRIMARY KEY,
                call_record_uid  UUID NOT NULL,
                file_path        TEXT NOT NULL DEFAULT '',
                file_size        BIGINT NOT NULL DEFAULT 0,
                format           TEXT NOT NULL DEFAULT 'wav',
                duration_seconds INTEGER NOT NULL DEFAULT 0,
                created_at       TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS idx_recordings_call_uid ON local_recordings(call_record_uid);
            """, conn);
        await cmdRecs.ExecuteNonQueryAsync();
    }

    // ═══════════════════════════════════════
    // CAGRI KAYITLARI — CRUD
    // ═══════════════════════════════════════

    public async Task SaveCallRecordAsync(LocalCallRecord record)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("""
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
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("""
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
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT * FROM local_call_records WHERE uid = @uid", conn);
        cmd.Parameters.AddWithValue("uid", uid);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapCallRecord(reader);
    }

    public async Task<List<LocalCallRecord>> GetCallRecordsAsync(
        DateTime? from, DateTime? to, int page, int pageSize)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Dinamik WHERE olustur (tarih filtresi opsiyonel)
        var where = "WHERE 1=1";
        if (from.HasValue) where += " AND started_at >= @from";
        if (to.HasValue) where += " AND started_at <= @to";

        var sql = $"""
            SELECT * FROM local_call_records
            {where}
            ORDER BY started_at DESC
            LIMIT @limit OFFSET @offset
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        if (from.HasValue) cmd.Parameters.AddWithValue("from", from.Value);
        if (to.HasValue) cmd.Parameters.AddWithValue("to", to.Value);
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        return await ReadCallRecordListAsync(cmd);
    }

    /// <summary>
    /// Backend'e henuz gonderilmemis kayitlari getir.
    /// BackgroundSyncService periyodik olarak bunu cagirir ve push eder.
    /// </summary>
    public async Task<List<LocalCallRecord>> GetUnsyncedRecordsAsync(int limit = 50)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("""
            SELECT * FROM local_call_records
            WHERE is_synced = FALSE
            ORDER BY started_at ASC
            LIMIT @limit
            """, conn);
        cmd.Parameters.AddWithValue("limit", limit);

        return await ReadCallRecordListAsync(cmd);
    }

    /// <summary>
    /// Kaydi "senkronlandi" olarak isaretle.
    /// Backend push basariliysa BackgroundSyncService bunu cagirir.
    /// </summary>
    public async Task MarkAsSyncedAsync(Guid uid, int? backendCallId = null)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("""
            UPDATE local_call_records SET
                is_synced = TRUE,
                last_sync_attempt = @now,
                backend_call_id = @backendId
            WHERE uid = @uid
            """, conn);
        cmd.Parameters.AddWithValue("uid", uid);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("backendId", (object?)backendCallId ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    // ═══════════════════════════════════════
    // SES KAYITLARI
    // ═══════════════════════════════════════

    public async Task SaveRecordingMetadataAsync(LocalRecording recording)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("""
            INSERT INTO local_recordings
                (uid, call_record_uid, file_path, file_size, format, duration_seconds, created_at)
            VALUES
                (@uid, @callUid, @path, @size, @format, @duration, @created)
            """, conn);

        cmd.Parameters.AddWithValue("uid", recording.Uid);
        cmd.Parameters.AddWithValue("callUid", recording.CallRecordUid);
        cmd.Parameters.AddWithValue("path", recording.FilePath);
        cmd.Parameters.AddWithValue("size", recording.FileSize);
        cmd.Parameters.AddWithValue("format", recording.Format);
        cmd.Parameters.AddWithValue("duration", recording.DurationSeconds);
        cmd.Parameters.AddWithValue("created", recording.CreatedAt);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<LocalRecording>> GetRecordingsAsync(
        Guid? callRecordUid, int page, int pageSize)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var where = callRecordUid.HasValue ? "WHERE call_record_uid = @callUid" : "";
        var sql = $"""
            SELECT * FROM local_recordings
            {where}
            ORDER BY created_at DESC
            LIMIT @limit OFFSET @offset
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        if (callRecordUid.HasValue) cmd.Parameters.AddWithValue("callUid", callRecordUid.Value);
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        var list = new List<LocalRecording>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new LocalRecording
            {
                Uid = reader.GetGuid(reader.GetOrdinal("uid")),
                CallRecordUid = reader.GetGuid(reader.GetOrdinal("call_record_uid")),
                FilePath = reader.GetString(reader.GetOrdinal("file_path")),
                FileSize = reader.GetInt64(reader.GetOrdinal("file_size")),
                Format = reader.GetString(reader.GetOrdinal("format")),
                DurationSeconds = reader.GetInt32(reader.GetOrdinal("duration_seconds")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            });
        }
        return list;
    }

    // ═══════════════════════════════════════
    // ISTATISTIKLER
    // ═══════════════════════════════════════

    public async Task<LocalCallStats> GetStatsAsync(DateTime from, DateTime to)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("""
            SELECT
                COUNT(*) AS total,
                COUNT(*) FILTER (WHERE status = 'Completed') AS answered,
                COUNT(*) FILTER (WHERE status = 'Missed') AS missed,
                COALESCE(SUM(duration_seconds) FILTER (WHERE status = 'Completed'), 0) AS total_duration
            FROM local_call_records
            WHERE started_at >= @from AND started_at <= @to
            """, conn);
        cmd.Parameters.AddWithValue("from", from);
        cmd.Parameters.AddWithValue("to", to);

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
    /// Connection string'den database adini parse edip, yoksa olusturur.
    /// postgres default DB'sine baglanir → CREATE DATABASE yapar.
    /// Zaten varsa hata vermez (IF NOT EXISTS mantigi).
    /// </summary>
    private async Task EnsureDatabaseExistsAsync()
    {
        // Connection string'den database adini cikar
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        var targetDb = builder.Database;

        if (string.IsNullOrWhiteSpace(targetDb))
            return; // database adi yoksa birsey yapma

        // postgres (default) DB'sine baglan
        builder.Database = "postgres";
        var adminConnStr = builder.ToString();

        await using var conn = new NpgsqlConnection(adminConnStr);
        await conn.OpenAsync();

        // Database var mi kontrol et
        await using var checkCmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @dbName", conn);
        checkCmd.Parameters.AddWithValue("dbName", targetDb);
        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists == null)
        {
            // Database yok — olustur
            // NOT: CREATE DATABASE parametrize edilemez, string interpolation kullaniyoruz.
            // SQL injection riski yok cunku deger kullanicinin kendi ayar ekranindan geliyor.
            var safeName = targetDb.Replace("\"", "\"\""); // cift tirnak escape
            await using var createCmd = new NpgsqlCommand(
                $"CREATE DATABASE \"{safeName}\"", conn);
            await createCmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// NpgsqlCommand'a LocalCallRecord parametrelerini ekler.
    /// Insert ve Update icin ortak kullanilir — kod tekrarini onler.
    /// </summary>
    private static void AddCallRecordParams(NpgsqlCommand cmd, LocalCallRecord r)
    {
        cmd.Parameters.AddWithValue("uid", r.Uid);
        cmd.Parameters.AddWithValue("caller", r.CallerNumber);
        cmd.Parameters.AddWithValue("callee", r.CalleeNumber);
        cmd.Parameters.AddWithValue("dir", r.Direction);
        cmd.Parameters.AddWithValue("status", r.Status);
        cmd.Parameters.AddWithValue("started", r.StartedAt);
        cmd.Parameters.AddWithValue("answered", (object?)r.AnsweredAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("ended", (object?)r.EndedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("duration", r.DurationSeconds);
        cmd.Parameters.AddWithValue("agent", (object?)r.AgentName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("queue", (object?)r.QueueName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("notes", (object?)r.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("recPath", (object?)r.RecordingFilePath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("synced", r.IsSyncedToBackend);
        cmd.Parameters.AddWithValue("lastSync", (object?)r.LastSyncAttempt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("backendId", (object?)r.BackendCallId ?? DBNull.Value);
    }

    /// <summary>
    /// NpgsqlDataReader'dan LocalCallRecord olusturur.
    /// Tum SELECT sonuclari icin ortak mapper.
    /// </summary>
    private static LocalCallRecord MapCallRecord(NpgsqlDataReader reader)
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

    /// <summary>
    /// Bir NpgsqlCommand'i calistirip sonuclari LocalCallRecord listesine donusturur.
    /// </summary>
    private static async Task<List<LocalCallRecord>> ReadCallRecordListAsync(NpgsqlCommand cmd)
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

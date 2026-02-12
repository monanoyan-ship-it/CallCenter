using CallCenter.Windows.LocalData.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CallCenter.Windows.LocalData.Providers;

/// <summary>
/// MongoDB tabanli lokal repository.
/// Document-based yapida calisir — SQL tablolari yerine koleksiyonlar kullanir.
///
/// Koleksiyonlar:
///   - call_records   → LocalCallRecord
///   - recordings     → LocalRecording
///
/// Kullanim:
///   var repo = new MongoLocalRepository("mongodb://localhost:27017/callcenter_local");
///   await repo.InitializeAsync();   // index'leri olustur
///   await repo.SaveCallRecordAsync(record);
///
/// Not: MongoDB connection string'inde veritabani adi yer almalidir.
///      Ornek: "mongodb://localhost:27017/callcenter_local"
/// </summary>
public class MongoLocalRepository : ILocalRepository
{
    private readonly string _connectionString;
    private IMongoDatabase? _db;

    // Koleksiyon isimleri — sabit, degismez
    private const string CallRecordsCollection = "call_records";
    private const string RecordingsCollection = "recordings";

    public MongoLocalRepository(string connectionString)
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
            var db = GetDatabase();
            // Veritabanina ping at — baglanti kontrolu
            await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Koleksiyon index'lerini olusturur.
    /// MongoDB'de tablo yaratmaya gerek yok — ilk insert'te otomatik olusur.
    /// Ama index'leri onceden tanimlamak performans icin onemli.
    /// </summary>
    public async Task InitializeAsync()
    {
        var db = GetDatabase();

        // ── call_records index'leri ──
        var callRecords = db.GetCollection<BsonDocument>(CallRecordsCollection);
        await callRecords.Indexes.CreateManyAsync([
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Descending("started_at"),
                new CreateIndexOptions { Name = "idx_started_at" }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("is_synced"),
                new CreateIndexOptions { Name = "idx_is_synced" })
        ]);

        // ── recordings index'leri ──
        var recordings = db.GetCollection<BsonDocument>(RecordingsCollection);
        await recordings.Indexes.CreateManyAsync([
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("call_record_uid"),
                new CreateIndexOptions { Name = "idx_call_record_uid" })
        ]);
    }

    // ═══════════════════════════════════════
    // CAGRI KAYITLARI — CRUD
    // ═══════════════════════════════════════

    public async Task SaveCallRecordAsync(LocalCallRecord record)
    {
        var collection = GetCallRecordCollection();
        var doc = CallRecordToDocument(record);
        await collection.InsertOneAsync(doc);
    }

    public async Task UpdateCallRecordAsync(LocalCallRecord record)
    {
        var collection = GetCallRecordCollection();
        var filter = Builders<BsonDocument>.Filter.Eq("_id", record.Uid.ToString());
        var doc = CallRecordToDocument(record);

        // Uid (_id) disindaki tum alanlari guncelle
        await collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = false });
    }

    public async Task<LocalCallRecord?> GetCallRecordByUidAsync(Guid uid)
    {
        var collection = GetCallRecordCollection();
        var filter = Builders<BsonDocument>.Filter.Eq("_id", uid.ToString());
        var doc = await collection.Find(filter).FirstOrDefaultAsync();
        return doc == null ? null : DocumentToCallRecord(doc);
    }

    public async Task<List<LocalCallRecord>> GetCallRecordsAsync(
        DateTime? from, DateTime? to, int page, int pageSize)
    {
        var collection = GetCallRecordCollection();

        // Filtre olustur — tarih araligi opsiyonel
        var filterBuilder = Builders<BsonDocument>.Filter;
        var filter = filterBuilder.Empty;
        if (from.HasValue) filter &= filterBuilder.Gte("started_at", from.Value);
        if (to.HasValue) filter &= filterBuilder.Lte("started_at", to.Value);

        var docs = await collection
            .Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Descending("started_at"))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return docs.Select(DocumentToCallRecord).ToList();
    }

    public async Task<List<LocalCallRecord>> GetUnsyncedRecordsAsync(int limit = 50)
    {
        var collection = GetCallRecordCollection();
        var filter = Builders<BsonDocument>.Filter.Eq("is_synced", false);

        var docs = await collection
            .Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Ascending("started_at"))
            .Limit(limit)
            .ToListAsync();

        return docs.Select(DocumentToCallRecord).ToList();
    }

    public async Task MarkAsSyncedAsync(Guid uid, int? backendCallId = null)
    {
        var collection = GetCallRecordCollection();
        var filter = Builders<BsonDocument>.Filter.Eq("_id", uid.ToString());
        var update = Builders<BsonDocument>.Update
            .Set("is_synced", true)
            .Set("last_sync_attempt", DateTime.UtcNow)
            .Set("backend_call_id", backendCallId.HasValue ? (BsonValue)backendCallId.Value : BsonNull.Value);

        await collection.UpdateOneAsync(filter, update);
    }

    // ═══════════════════════════════════════
    // SES KAYITLARI
    // ═══════════════════════════════════════

    public async Task SaveRecordingMetadataAsync(LocalRecording recording)
    {
        var collection = GetRecordingCollection();
        var doc = new BsonDocument
        {
            { "_id", recording.Uid.ToString() },
            { "call_record_uid", recording.CallRecordUid.ToString() },
            { "file_path", recording.FilePath },
            { "file_size", recording.FileSize },
            { "format", recording.Format },
            { "duration_seconds", recording.DurationSeconds },
            { "created_at", recording.CreatedAt }
        };
        await collection.InsertOneAsync(doc);
    }

    public async Task<List<LocalRecording>> GetRecordingsAsync(
        Guid? callRecordUid, int page, int pageSize)
    {
        var collection = GetRecordingCollection();

        var filter = callRecordUid.HasValue
            ? Builders<BsonDocument>.Filter.Eq("call_record_uid", callRecordUid.Value.ToString())
            : Builders<BsonDocument>.Filter.Empty;

        var docs = await collection
            .Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Descending("created_at"))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return docs.Select(doc => new LocalRecording
        {
            Uid = Guid.Parse(doc["_id"].AsString),
            CallRecordUid = Guid.Parse(doc["call_record_uid"].AsString),
            FilePath = doc["file_path"].AsString,
            FileSize = doc["file_size"].ToInt64(),
            Format = doc["format"].AsString,
            DurationSeconds = doc["duration_seconds"].ToInt32(),
            CreatedAt = doc["created_at"].ToUniversalTime()
        }).ToList();
    }

    // ═══════════════════════════════════════
    // ISTATISTIKLER
    // ═══════════════════════════════════════

    /// <summary>
    /// MongoDB Aggregation Pipeline ile istatistik hesaplar.
    /// SQL'deki GROUP BY + COUNT + SUM'in MongoDB karsiligi.
    /// </summary>
    public async Task<LocalCallStats> GetStatsAsync(DateTime from, DateTime to)
    {
        var collection = GetCallRecordCollection();

        // Aggregation pipeline: match → group → project
        var pipeline = new BsonDocument[]
        {
            // 1. Tarih filtresi
            new("$match", new BsonDocument
            {
                { "started_at", new BsonDocument
                    {
                        { "$gte", from },
                        { "$lte", to }
                    }
                }
            }),
            // 2. Grup: tum kayitlari tek bir satira topla
            new("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "total", new BsonDocument("$sum", 1) },
                { "answered", new BsonDocument("$sum",
                    new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$eq", new BsonArray { "$status", "Completed" }),
                        1, 0
                    }))
                },
                { "missed", new BsonDocument("$sum",
                    new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$eq", new BsonArray { "$status", "Missed" }),
                        1, 0
                    }))
                },
                { "total_duration", new BsonDocument("$sum",
                    new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$eq", new BsonArray { "$status", "Completed" }),
                        "$duration_seconds", 0
                    }))
                }
            })
        };

        var results = await collection
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        if (results.Count == 0) return new LocalCallStats();

        var result = results[0];
        return new LocalCallStats
        {
            TotalCalls = result["total"].ToInt32(),
            AnsweredCalls = result["answered"].ToInt32(),
            MissedCalls = result["missed"].ToInt32(),
            TotalDurationSeconds = result["total_duration"].ToInt32()
        };
    }

    // ═══════════════════════════════════════
    // YARDIMCI METODLAR
    // ═══════════════════════════════════════

    /// <summary>
    /// Connection string'den veritabani adini cikarir ve IMongoDatabase dondurur.
    /// Lazy initialization: ilk cagrildiginda olusturur, sonraki cagrilarda cache'den dondurur.
    /// </summary>
    private IMongoDatabase GetDatabase()
    {
        if (_db != null) return _db;

        var mongoUrl = new MongoUrl(_connectionString);
        var client = new MongoClient(mongoUrl);

        // Veritabani adi connection string'den cekilir
        // Ornek: "mongodb://localhost:27017/callcenter_local" → "callcenter_local"
        var dbName = mongoUrl.DatabaseName ?? "callcenter_local";
        _db = client.GetDatabase(dbName);
        return _db;
    }

    private IMongoCollection<BsonDocument> GetCallRecordCollection()
        => GetDatabase().GetCollection<BsonDocument>(CallRecordsCollection);

    private IMongoCollection<BsonDocument> GetRecordingCollection()
        => GetDatabase().GetCollection<BsonDocument>(RecordingsCollection);

    /// <summary>
    /// LocalCallRecord → BsonDocument donusumu.
    /// MongoDB'de _id alani primary key olarak Uid kullanilir.
    /// </summary>
    private static BsonDocument CallRecordToDocument(LocalCallRecord r)
    {
        return new BsonDocument
        {
            { "_id", r.Uid.ToString() },
            { "caller_number", r.CallerNumber },
            { "callee_number", r.CalleeNumber },
            { "direction", r.Direction },
            { "status", r.Status },
            { "started_at", r.StartedAt },
            { "answered_at", r.AnsweredAt.HasValue ? (BsonValue)r.AnsweredAt.Value : BsonNull.Value },
            { "ended_at", r.EndedAt.HasValue ? (BsonValue)r.EndedAt.Value : BsonNull.Value },
            { "duration_seconds", r.DurationSeconds },
            { "agent_name", r.AgentName != null ? (BsonValue)r.AgentName : BsonNull.Value },
            { "queue_name", r.QueueName != null ? (BsonValue)r.QueueName : BsonNull.Value },
            { "notes", r.Notes != null ? (BsonValue)r.Notes : BsonNull.Value },
            { "recording_file_path", r.RecordingFilePath != null ? (BsonValue)r.RecordingFilePath : BsonNull.Value },
            { "is_synced", r.IsSyncedToBackend },
            { "last_sync_attempt", r.LastSyncAttempt.HasValue ? (BsonValue)r.LastSyncAttempt.Value : BsonNull.Value },
            { "backend_call_id", r.BackendCallId.HasValue ? (BsonValue)r.BackendCallId.Value : BsonNull.Value }
        };
    }

    /// <summary>
    /// BsonDocument → LocalCallRecord donusumu.
    /// Null alanlar icin BsonNull kontrolu yapilir.
    /// </summary>
    private static LocalCallRecord DocumentToCallRecord(BsonDocument doc)
    {
        return new LocalCallRecord
        {
            Uid = Guid.Parse(doc["_id"].AsString),
            CallerNumber = doc["caller_number"].AsString,
            CalleeNumber = doc["callee_number"].AsString,
            Direction = doc["direction"].AsString,
            Status = doc["status"].AsString,
            StartedAt = doc["started_at"].ToUniversalTime(),
            AnsweredAt = doc["answered_at"].IsBsonNull ? null : doc["answered_at"].ToUniversalTime(),
            EndedAt = doc["ended_at"].IsBsonNull ? null : doc["ended_at"].ToUniversalTime(),
            DurationSeconds = doc["duration_seconds"].ToInt32(),
            AgentName = doc["agent_name"].IsBsonNull ? null : doc["agent_name"].AsString,
            QueueName = doc["queue_name"].IsBsonNull ? null : doc["queue_name"].AsString,
            Notes = doc["notes"].IsBsonNull ? null : doc["notes"].AsString,
            RecordingFilePath = doc["recording_file_path"].IsBsonNull ? null : doc["recording_file_path"].AsString,
            IsSyncedToBackend = doc["is_synced"].ToBoolean(),
            LastSyncAttempt = doc["last_sync_attempt"].IsBsonNull ? null : doc["last_sync_attempt"].ToUniversalTime(),
            BackendCallId = doc["backend_call_id"].IsBsonNull ? null : doc["backend_call_id"].ToInt32()
        };
    }
}

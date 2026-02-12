namespace CallCenter.Shared.DTOs;

/// <summary>Yeni arama baslatma request'i</summary>
public class StartCallRequest
{
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int? QueueId { get; set; }
}

/// <summary>Arama baslat response'u (Id + Uid)</summary>
public class CallStartResult
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
}

/// <summary>Gelen arama request'i (PBX/webhook'tan)</summary>
public class IncomingCallRequest
{
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int? QueueId { get; set; }
}

/// <summary>
/// Windows uygulamasinin lokal DB'den backend'e senkronladigi cagri verisi.
/// Uid bazli idempotent — ayni Uid tekrar gelirse gunceller, yeni kayit olusturmaz.
/// BackgroundSyncService tarafindan kullanilir.
/// </summary>
public class CallSyncPushRequest
{
    public Guid Uid { get; set; }
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public string Direction { get; set; } = "Outbound";
    public string Status { get; set; } = "Completed";
    public DateTime StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; }
    public string? AgentName { get; set; }
    public string? QueueName { get; set; }
    public string? Notes { get; set; }

    /// <summary>Musterinin lokal makinesindeki ses kaydi dosya yolu (ses dosyasi gonderilmez, sadece path bilgisi)</summary>
    public string? RecordingFilePath { get; set; }
}

/// <summary>Backend'in sync push'a verdigi yanit</summary>
public class CallSyncPushResponse
{
    public int Id { get; set; }
    public bool IsNew { get; set; }
}

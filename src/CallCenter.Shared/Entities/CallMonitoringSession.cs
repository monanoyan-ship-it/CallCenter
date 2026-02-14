namespace CallCenter.Shared.Entities;

/// <summary>
/// Arama izleme oturumu. Supervisor bir agent'in aramasini dinler/fisilda/barge yapar.
/// Medya sunucusu entegre edildiginde gercek ses akisi olacak; su an DB kaydi.
/// </summary>
public class CallMonitoringSession
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Izlenen arama</summary>
    public int CallRecordId { get; set; }
    public CallRecord? CallRecord { get; set; }

    /// <summary>Izleyen supervisor</summary>
    public int SupervisorId { get; set; }
    public User? Supervisor { get; set; }

    /// <summary>Izleme modu: Silent, Whisper, Barge</summary>
    public int ModeId { get; set; } = 1; // MonitoringModes.Ids.Silent

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    /// <summary>Supervisor notlari</summary>
    public string? Notes { get; set; }
}

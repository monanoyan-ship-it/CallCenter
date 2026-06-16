namespace CallCenter.Shared.DTOs;

/// <summary>
/// WinApp -> API: eslesmemis (Uid'siz) bir ses kaydini buluta yukledikten sonra kaydeder.
/// </summary>
public class RegisterOrphanRecordingRequest
{
    public string FileName { get; set; } = string.Empty;
    public DateTime? RecordedAt { get; set; }
    public long FileSize { get; set; }
    public string? FileHash { get; set; }
    public bool IsEncrypted { get; set; }
    public string? CloudFileId { get; set; }
    public string? CloudFileName { get; set; }
    public string? PlatformFileId { get; set; }
    public string? AgentName { get; set; }
    public string? MachineId { get; set; }
    public DateTime? RetentionDate { get; set; }
}

/// <summary>Eslesmemis kayitlar listesi/inbox icin.</summary>
public class OrphanRecordingDto
{
    public Guid Uid { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime? RecordedAt { get; set; }
    public long FileSize { get; set; }
    public bool IsEncrypted { get; set; }
    public bool HasCloudFile { get; set; }
    public string? AgentName { get; set; }
    public string? MachineId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Orphan kaydi dogru cagriya eslerken hedef cagri.</summary>
public class MatchOrphanRequest
{
    public Guid CallUid { get; set; }
}

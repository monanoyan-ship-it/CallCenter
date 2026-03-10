namespace CallCenter.Shared.DTOs;

/// <summary>Kayit bilgisi (yetki kontrolu sonrasi dondurulur)</summary>
public class RecordingInfoDto
{
    public bool HasRecording { get; set; }
    public bool IsEncrypted { get; set; }
    public long? FileSize { get; set; }
    public int? DurationSeconds { get; set; }
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public DateTime CallDate { get; set; }
}

/// <summary>Windows app icin cift upload hedefleri</summary>
public class RecordingUploadTargetsDto
{
    public bool UploadToPlatform { get; set; }
    public CloudConfigForClientDto? PlatformConfig { get; set; }
    public bool UploadToCustomerStorage { get; set; }
    public CloudConfigForClientDto? CustomerConfig { get; set; }
    public bool AutoRecordCalls { get; set; }
}

public class ToggleAutoRecordRequest
{
    public bool Enabled { get; set; }
}

/// <summary>Oturum acik kullanici bilgisi (yetki kontrolu icin)</summary>
public class CurrentUserInfo
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsCustomerAdmin { get; set; }
    public bool IsSupervisor { get; set; }
    public int? PersonnelId { get; set; }
}

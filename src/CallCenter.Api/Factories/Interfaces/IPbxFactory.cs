namespace CallCenter.Api.Factories.Interfaces;

/// <summary>
/// PbxService icin ozel API islemleri.
/// PbxService DB'ye erismez, her seyi bu endpoint'ler uzerinden alir.
/// </summary>
public interface IPbxFactory
{
    // Konfigürasyon
    Task<PbxTrunkListResult> GetTrunksAsync(string customerUid);
    Task<PbxCustomerConfigResult?> GetCustomerConfigAsync(string customerUid);
    Task<PbxBusinessHoursResult?> GetBusinessHoursAsync(string customerUid);
    Task<PbxIvrMenuResult?> GetIvrMenuAsync(int menuId, string customerUid);
    Task<PbxQueueConfigResult?> GetQueueConfigAsync(int queueId);

    // Çağrı yönetimi
    Task<PbxAvailableAgentResult?> GetAvailableAgentAsync(int queueId);
    Task<PbxCallCreatedResult?> CreateIncomingCallAsync(PbxIncomingCallRequest request);
    Task<bool> UpdateCallRecordAsync(int callRecordId, PbxCallUpdateRequest request);

    // Ses dosyası
    Task<(byte[]? Data, string? ContentType)?> GetAudioFileAsync(int greetingMessageId);
}

// ---- PBX DTO'lar ----

public class PbxTrunkListResult
{
    public List<PbxTrunkDto> Trunks { get; set; } = [];
}

public class PbxTrunkDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 5060;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string Transport { get; set; } = "UDP";
    public bool UseSrtp { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PbxCustomerConfigResult
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? DefaultIvrMenuId { get; set; }
    public int? DefaultQueueId { get; set; }
    public bool HasBusinessHours { get; set; }
}

public class PbxBusinessHoursResult
{
    public List<PbxWorkdayDto> Workdays { get; set; } = [];
    public List<PbxHolidayDto> Holidays { get; set; } = [];
    public string Timezone { get; set; } = "Europe/Istanbul";
}

public class PbxWorkdayDto
{
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkday { get; set; }
}

public class PbxHolidayDto
{
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public int? GreetingMessageId { get; set; }
}

public class PbxIvrMenuResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? GreetingMessageId { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 5;
    public List<PbxIvrOptionDto> Options { get; set; } = [];
}

public class PbxIvrOptionDto
{
    public string Digit { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public int? TargetQueueId { get; set; }
    public string? TargetExtension { get; set; }
    public int? TargetIvrMenuId { get; set; }
    public int? TargetGreetingMessageId { get; set; }
}

public class PbxQueueConfigResult
{
    public int QueueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? HoldMusicId { get; set; }
    public int? TimeoutMessageId { get; set; }
    public int MaxWaitTimeSeconds { get; set; } = 600;
    public int MaxQueueLength { get; set; } = 50;
}

public class PbxAvailableAgentResult
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string SipExtension { get; set; } = string.Empty;
    public string? SipServer { get; set; }
    public int SipPort { get; set; } = 5060;
}

public class PbxIncomingCallRequest
{
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public string SipCallId { get; set; } = string.Empty;
    public int? QueueId { get; set; }
    public string? CustomerUid { get; set; }
}

public class PbxCallCreatedResult
{
    public int Id { get; set; }
}

public class PbxCallUpdateRequest
{
    public int? StatusId { get; set; }
    public int? AgentId { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
}

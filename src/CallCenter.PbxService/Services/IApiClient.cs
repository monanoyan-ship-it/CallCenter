namespace CallCenter.PbxService.Services;

/// <summary>
/// PbxService -> API iletisim katmani.
/// Tum veri API uzerinden gelir, DB erisimi yok.
/// </summary>
public interface IApiClient
{
    Task<List<TrunkInfo>> GetSipAccountsAsync(string customerUid);
    Task<CustomerPbxConfig?> GetCustomerPbxConfigAsync(string customerUid);
    Task<BusinessHoursInfo?> GetBusinessHoursAsync(string customerUid);
    Task<IvrMenuInfo?> GetIvrMenuAsync(string customerUid, int menuId);
    Task<QueueConfigInfo?> GetQueueConfigAsync(int queueId);
    Task<AvailableAgentInfo?> GetAvailableAgentAsync(int queueId);
    Task<int?> CreateIncomingCallRecordAsync(IncomingCallRequest request);
    Task UpdateCallRecordAsync(int callRecordId, CallRecordUpdate update);
}

/// <summary>ACD tarafindan bulunan musait agent bilgisi</summary>
public class AvailableAgentInfo
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string SipExtension { get; set; } = string.Empty;
    public string? SipServer { get; set; }
    public int SipPort { get; set; } = 5060;
}

/// <summary>SIP trunk (GoIP / SIP provider) bilgileri - API'den gelir</summary>
public class TrunkInfo
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

/// <summary>Musterinin PBX yapilandirmasi - API'den gelir</summary>
public class CustomerPbxConfig
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? DefaultIvrMenuId { get; set; }
    public int? DefaultQueueId { get; set; }
    public bool HasBusinessHours { get; set; }
}

/// <summary>Mesai saati bilgisi - API'den gelir</summary>
public class BusinessHoursInfo
{
    public List<WorkdayInfo> Workdays { get; set; } = [];
    public List<HolidayInfo> Holidays { get; set; } = [];
    public string Timezone { get; set; } = "Europe/Istanbul";
}

public class WorkdayInfo
{
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkday { get; set; }
}

public class HolidayInfo
{
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public int? GreetingMessageId { get; set; }
}

/// <summary>Gelen cagri kaydi olusturma istegi</summary>
public class IncomingCallRequest
{
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public string SipCallId { get; set; } = string.Empty;
    public int? QueueId { get; set; }
}

/// <summary>Cagri kaydi guncelleme</summary>
public class CallRecordUpdate
{
    public int? StatusId { get; set; }
    public int? AgentId { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
}

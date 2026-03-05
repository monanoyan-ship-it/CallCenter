namespace CallCenter.PbxService.Services;

/// <summary>
/// PbxService -> API iletisim katmani.
/// Tum veri API uzerinden gelir, DB erisimi yok.
/// </summary>
public interface IApiClient
{
    Task<List<TrunkInfo>> GetSipAccountsAsync(string customerUid);
    Task<CustomerPbxConfig?> GetCustomerPbxConfigAsync(string customerUid);
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

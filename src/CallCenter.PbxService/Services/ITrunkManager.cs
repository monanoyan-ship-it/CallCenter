namespace CallCenter.PbxService.Services;

public interface ITrunkManager
{
    Task LoadTrunksAsync(string customerUid);
    Task RegisterAllAsync();
    Task UnregisterAllAsync();
    TrunkState? GetTrunkState(int sipAccountId);
    IReadOnlyCollection<TrunkState> GetAllTrunkStates();
    int RegisteredTrunkCount { get; }
}

public class TrunkState
{
    public int SipAccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Server { get; init; } = string.Empty;
    public TrunkStatus Status { get; set; } = TrunkStatus.Disconnected;
    public DateTime LastRegisteredAt { get; set; }
    public DateTime? LastFailedAt { get; set; }
    public string? LastError { get; set; }
    public int FailCount { get; set; }
}

public enum TrunkStatus
{
    Disconnected,
    Registering,
    Registered,
    Failed
}

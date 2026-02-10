namespace CallCenter.Shared.DTOs;

/// <summary>Yeni arama baslatma request'i</summary>
public class StartCallRequest
{
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int? QueueId { get; set; }
}

/// <summary>Gelen arama request'i (PBX/webhook'tan)</summary>
public class IncomingCallRequest
{
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int? QueueId { get; set; }
}

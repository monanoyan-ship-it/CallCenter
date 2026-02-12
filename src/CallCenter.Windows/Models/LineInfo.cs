namespace CallCenter.Windows.Models;

/// <summary>
/// Multi-line arama hat bilgisi.
/// MicroSIP'teki gibi birden fazla esanli hat destegi.
/// </summary>
public class LineInfo
{
    public int Index { get; set; }
    public bool IsActive { get; set; }
    public bool IsOnHold { get; set; }
    public bool IsRinging { get; set; }
    public string? RemoteParty { get; set; }
    public string? RemoteDisplayName { get; set; }
    public DateTime? StartTime { get; set; }
    public LineState State { get; set; } = LineState.Idle;
}

public enum LineState
{
    Idle,
    Ringing,
    Connecting,
    Connected,
    OnHold,
    TransferPending
}

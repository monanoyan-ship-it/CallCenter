namespace CallCenter.Windows.Models;

/// <summary>
/// Ses cihazi bilgi modeli.
/// </summary>
public class AudioDeviceInfo
{
    public int DeviceIndex { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    /// <summary>"input" (mikrofon) veya "output" (hoparlor)</summary>
    public string Kind { get; set; } = "input";
}

namespace CallCenter.Windows.Models;

/// <summary>
/// SIP ses codec bilgisi.
/// Kullanici UI'dan codec siralama/etkinlestirme yapabilir.
/// </summary>
public class CodecInfo
{
    public string Name { get; set; } = string.Empty;
    public int PayloadType { get; set; }
    public int SampleRate { get; set; }
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }
}

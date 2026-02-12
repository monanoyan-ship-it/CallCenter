namespace CallCenter.Windows.LocalData.Entities;

/// <summary>
/// Lokal raporlama icin istatistik ozeti.
/// ILocalRepository.GetStatsAsync() donusu.
/// </summary>
public class LocalCallStats
{
    public int TotalCalls { get; set; }
    public int AnsweredCalls { get; set; }
    public int MissedCalls { get; set; }
    public int TotalDurationSeconds { get; set; }

    /// <summary>Ortalama gorusme suresi (saniye) — cevaplanan aramalar icin</summary>
    public double AvgDurationSeconds => AnsweredCalls > 0
        ? (double)TotalDurationSeconds / AnsweredCalls
        : 0;
}

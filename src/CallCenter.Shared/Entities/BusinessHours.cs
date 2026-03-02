namespace CallCenter.Shared.Entities;

/// <summary>
/// Musteri calisma saatleri. Her gun icin baslangic ve bitis saati.
/// Mesai disi gelen aramalarda farkli karsilama mesaji calinir.
/// </summary>
public class BusinessHours
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    /// <summary>0=Pazar, 1=Pazartesi, ..., 6=Cumartesi</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Baslangic saati (orn: 09:00)</summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>Bitis saati (orn: 18:00)</summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>Bu gun calisma gunu mu</summary>
    public bool IsWorkday { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Customer Customer { get; set; } = null!;
}

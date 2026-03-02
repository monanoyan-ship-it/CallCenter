namespace CallCenter.Shared.Entities;

/// <summary>
/// Kuyruk bekleme muzigi.
/// Agent musait degilken arayan kisiye calan muzik.
/// </summary>
public class HoldMusic
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public int CustomerId { get; set; }

    /// <summary>Belirli bir kuyruga ozel (null = tum kuyruklar icin varsayilan)</summary>
    public int? QueueId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string AudioFilePath { get; set; } = string.Empty;
    public string? AudioFileName { get; set; }
    public int? DurationSeconds { get; set; }

    /// <summary>Bu musterinin varsayilan bekleme muzigi mi</summary>
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Customer Customer { get; set; } = null!;
    public Queue? Queue { get; set; }
}

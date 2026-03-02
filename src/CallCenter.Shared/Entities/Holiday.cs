namespace CallCenter.Shared.Entities;

/// <summary>
/// Tatil gunleri. Musteri bazli ozel tatil takvimi.
/// Tatil gunlerinde gelen aramalar icin ozel karsilama mesaji calinabilir.
/// </summary>
public class Holiday
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    /// <summary>Tatil tarihi</summary>
    public DateOnly Date { get; set; }

    /// <summary>Tatil adi: "Cumhuriyet Bayrami", "Kurban Bayrami" vb.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tatil gununde calinacak ozel karsilama mesaji (opsiyonel)</summary>
    public int? GreetingMessageId { get; set; }

    /// <summary>Her yil tekrarlanir mi</summary>
    public bool IsRecurring { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Customer Customer { get; set; } = null!;
    public GreetingMessage? GreetingMessage { get; set; }
}

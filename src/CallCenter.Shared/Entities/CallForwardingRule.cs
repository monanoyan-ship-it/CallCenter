namespace CallCenter.Shared.Entities;

/// <summary>
/// Arama yonlendirme kurali.
/// Type: Always (her zaman), Busy (mesgulde), NoAnswer (cevaplanmadığında), Offline (cevrimdisiyken)
/// </summary>
public class CallForwardingRule
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Kural sahibi kullanici</summary>
    public int UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Musteri (coklu musteri destegi icin)</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Yonlendirme tipi: Always=1, Busy=2, NoAnswer=3, Offline=4</summary>
    public int ForwardType { get; set; }

    /// <summary>Hedef numara veya SIP URI</summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>Kural aktif mi</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Oncelik sirasi (kucuk = yuksek oncelik)</summary>
    public int Priority { get; set; }

    /// <summary>NoAnswer tipi icin: kac saniye bekleyip yonlendirecek (varsayilan 20)</summary>
    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>Aciklama</summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Yonlendirme tipleri icin sabitler</summary>
public static class ForwardTypes
{
    public const int Always = 1;
    public const int Busy = 2;
    public const int NoAnswer = 3;
    public const int Offline = 4;

    public static readonly Dictionary<int, string> Names = new()
    {
        { Always, "Always" },
        { Busy, "Busy" },
        { NoAnswer, "NoAnswer" },
        { Offline, "Offline" }
    };

    public static string GetName(int type) =>
        Names.TryGetValue(type, out var name) ? name : "Unknown";
}

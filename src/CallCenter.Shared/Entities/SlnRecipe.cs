namespace CallCenter.Shared.Entities;

/// <summary>
/// Recete sablonu. Bir hizmetin uygulanmasi icin gereken malzeme/urun listesi.
/// Ornegin: "Balayaj Sari Formul" -> 30gr 6.0 boya + 20gr 7.1 boya + 45ml oksidan
/// </summary>
public class SlnRecipe
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }

    /// <summary>Bu recete hangi hizmet icin (opsiyonel)</summary>
    public int? ServiceId { get; set; }
    public SlnService? Service { get; set; }

    /// <summary>Tahmini toplam malzeme maliyeti (item larin toplamı)</summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>Referans fotograf URL (sonuc gorseli, renk ornegi vb.)</summary>
    public string? PhotoUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SlnRecipeItem> Items { get; set; } = [];
}

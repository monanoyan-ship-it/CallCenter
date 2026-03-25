namespace CallCenter.Shared.Entities;

/// <summary>
/// Kalite Puan Eşik Değerleri - Müşteri bazlı başarı/uyarı eşikleri.
/// SecretCustomer CustomerScoreThreshold entity'sinden adapte edildi.
/// Örnek: %80 üstü = Başarılı (yeşil), %60-80 = Uyarı (sarı), %60 altı = Başarısız (kırmızı)
/// </summary>
public class CrmQualityScoreThreshold
{
    public int Id { get; set; }

    /// <summary>Hangi müşteriye ait</summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Başarı eşiği yüzdesi (bu ve üstü = başarılı, yeşil)</summary>
    public decimal SuccessThreshold { get; set; } = 80;

    /// <summary>Uyarı eşiği yüzdesi (bu ve üstü ama success altı = uyarı, sarı)</summary>
    public decimal WarningThreshold { get; set; } = 60;

    /// <summary>Hangi checklist için (null ise tüm checklist'ler için geçerli)</summary>
    public int? ChecklistId { get; set; }
    public CrmQualityChecklist? Checklist { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

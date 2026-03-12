namespace CallCenter.Shared.Entities;

/// <summary>
/// Kalite Değerlendirme Kontrol Listesi - Çağrı kalitesini ölçmek için kullanılan şablon.
/// SecretCustomer Checklist entity'sinden adapte edildi (CallPerformance tipi).
/// Her müşteri kendi checklist'lerini oluşturabilir.
/// </summary>
public class QualityChecklist
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Hangi müşteriye ait</summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Kontrol listesi adı (ör: "Satış Çağrısı Değerlendirme Formu")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Açıklama</summary>
    public string? Description { get; set; }

    /// <summary>Puanlı mı? (false ise sadece checklist, puan hesaplanmaz)</summary>
    public bool IsScored { get; set; } = true;

    /// <summary>Aktif mi?</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Versiyon numarası (güncelleme takibi)</summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Puanlama yöntemi (QualityScoringMethods.Ids)
    /// Maximum: (GivenPoints / MaxPoints) * WeightPoints
    /// CriteriaTotal: Seçenek puanı direkt sayılır
    /// </summary>
    public int ScoringMethodId { get; set; } = Enums.QualityScoringMethods.Ids.Maximum;

    /// <summary>Maksimum alınabilecek toplam puan (varsayılan 100)</summary>
    public decimal MaxTotalPoints { get; set; } = 100;

    /// <summary>Kontrol listesi kodu (benzersiz tanımlayıcı, ör: "KL-SATIS-001")</summary>
    public string? Code { get; set; }

    /// <summary>Geçerlilik başlangıç tarihi</summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>Geçerlilik bitiş tarihi</summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>Soru gruplarını gizle (formda grup isimleri görünmez, raporlamada kullanılır)</summary>
    public bool HideGroupNames { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<QualityQuestion> Questions { get; set; } = new List<QualityQuestion>();
    public ICollection<QualityEvaluation> Evaluations { get; set; } = new List<QualityEvaluation>();
}

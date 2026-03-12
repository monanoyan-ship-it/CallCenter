namespace CallCenter.Shared.Entities;

/// <summary>
/// Kalite Sorusu (Kriter) - Değerlendirme formundaki her bir kriter.
/// SecretCustomer Question entity'sinden adapte edildi.
/// Örnek: "Çağrı standartlarına uyum", "Aktif dinleme", "Ürün bilgisi" vb.
/// </summary>
public class QualityQuestion
{
    public int Id { get; set; }

    /// <summary>Hangi checklist'e ait</summary>
    public int ChecklistId { get; set; }
    public QualityChecklist Checklist { get; set; } = null!;

    /// <summary>Soru/kriter metni (ör: "Müşteriyi ismiyle selamladı mı?")</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Sıralama</summary>
    public int Order { get; set; }

    /// <summary>
    /// Puanlama tipi (QualityScoringTypes.Ids)
    /// Scored=1: Normal puanlama
    /// Unscored=2: Puansız (sadece gözlem)
    /// Penalty=3: Ceza (sarı/kırmızı kart)
    /// </summary>
    public int ScoringTypeId { get; set; } = Enums.QualityScoringTypes.Ids.Scored;

    /// <summary>
    /// Ağırlık puanı - Sorunun toplam skora etkisi
    /// Varsayılan 10, 10 soru = 100 puan toplam
    /// </summary>
    public decimal WeightPoints { get; set; } = 10;

    /// <summary>
    /// Maksimum puan - Bu soru için verilebilecek maksimum puan (0'dan MaxPoints'e kadar)
    /// 1 = Evet/Hayır, 5 = Likert ölçeği
    /// </summary>
    public int MaxPoints { get; set; } = 5;

    /// <summary>
    /// Ceza tipi (QualityPenaltyTypes.Ids) - ScoringType=Penalty olduğunda kullanılır
    /// None=0, YellowCard=1, RedCard=2
    /// </summary>
    public int PenaltyTypeId { get; set; } = Enums.QualityPenaltyTypes.Ids.None;

    /// <summary>Zorunlu mu? (zorunlu değilse "Gerekmedi" seçilebilir)</summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>Değerlendirici için yardımcı metin / ipucu</summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Soru grubu (ör: "İletişim", "Ürün Bilgisi", "Kapanış")
    /// Raporlamada kategori bazlı gruplama için kullanılır
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>Puan girişi gösterilsin mi? (false ise sadece alt kriter seçimi)</summary>
    public bool ShowScoreInput { get; set; } = true;

    /// <summary>Yorum yapılabilir mi?</summary>
    public bool AllowComment { get; set; } = true;

    /// <summary>Aktif mi?</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<QualityAnswer> Answers { get; set; } = new List<QualityAnswer>();
    public ICollection<QualityQuestionSubCriteria> SubCriteria { get; set; } = new List<QualityQuestionSubCriteria>();
}

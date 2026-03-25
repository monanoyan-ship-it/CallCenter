namespace CallCenter.Shared.Entities;

/// <summary>
/// Cevap Alt Kriter Seçimi - Değerlendirme sırasında seçilen puan kırılma nedenleri.
/// SecretCustomer AnswerSubCriteriaSelection entity'sinden adapte edildi.
/// Many-to-many: CrmQualityAnswer ↔ CrmQualityQuestionSubCriteria
/// </summary>
public class CrmQualityAnswerSubCriteriaSelection
{
    public int Id { get; set; }

    /// <summary>Hangi cevaba ait</summary>
    public int AnswerId { get; set; }
    public CrmQualityAnswer Answer { get; set; } = null!;

    /// <summary>Seçilen alt kriter</summary>
    public int SubCriteriaId { get; set; }
    public CrmQualityQuestionSubCriteria SubCriteria { get; set; } = null!;

    /// <summary>Seçim sırasında eklenen not (opsiyonel)</summary>
    public string? Notes { get; set; }

    /// <summary>Seçim tarihi</summary>
    public DateTime SelectedAt { get; set; } = DateTime.UtcNow;
}

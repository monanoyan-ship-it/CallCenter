namespace CallCenter.Shared.Entities;

/// <summary>
/// Soru Alt Kriteri - Puan kırılma nedenleri.
/// SecretCustomer QuestionSubCriteria entity'sinden adapte edildi.
/// Örnek: "İlgili davranılmadı", "İsim ile hitap etmedi", "Aktif dinleme yapmadı"
/// Değerlendirme sırasında seçilerek puan düşürme nedenini belgeler.
/// </summary>
public class QualityQuestionSubCriteria
{
    public int Id { get; set; }

    /// <summary>Hangi soruya ait</summary>
    public int QuestionId { get; set; }
    public QualityQuestion Question { get; set; } = null!;

    /// <summary>Alt kriter açıklaması (ör: "İlgili davranılmadı")</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Sıralama</summary>
    public int Order { get; set; }

    /// <summary>
    /// Ağırlık puanı - Bu alt kriter seçildiğinde düşürülecek puan
    /// Örn: 1 puan düşürme, 2 puan düşürme
    /// </summary>
    public decimal WeightPoints { get; set; } = 1;

    /// <summary>Aktif mi?</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<QualityAnswerSubCriteriaSelection> Selections { get; set; } = new List<QualityAnswerSubCriteriaSelection>();
}

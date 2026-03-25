namespace CallCenter.Shared.Entities;

/// <summary>
/// Kalite Değerlendirme Cevabı - Her soru için verilen cevap ve puan.
/// SecretCustomer Answer entity'sinden adapte edildi.
/// Puanlama formülü (Maximum method): EarnedPoints = (GivenPoints / MaxPoints) * WeightPoints
/// </summary>
public class CrmQualityAnswer
{
    public int Id { get; set; }

    /// <summary>Hangi değerlendirmeye ait</summary>
    public int EvaluationId { get; set; }
    public CrmQualityEvaluation Evaluation { get; set; } = null!;

    /// <summary>Hangi soruya cevap</summary>
    public int QuestionId { get; set; }
    public CrmQualityQuestion Question { get; set; } = null!;

    /// <summary>
    /// Verilen ham puan (0 - MaxPoints arası)
    /// Supervisor'ın girdiği puan değeri
    /// </summary>
    public decimal? GivenPoints { get; set; }

    /// <summary>
    /// Kazanılan hesaplanmış puan
    /// Maximum method: (GivenPoints / MaxPoints) * WeightPoints
    /// </summary>
    public decimal? EarnedPoints { get; set; }

    /// <summary>Metin cevabı (yorumsuz sorular için)</summary>
    public string? AnswerText { get; set; }

    /// <summary>Değerlendirici notu (bu soru için)</summary>
    public string? Notes { get; set; }

    /// <summary>Öneri notu (agent'a iyileştirme önerisi)</summary>
    public string? RecommendationNotes { get; set; }

    /// <summary>Ceza uygulandı mı? (cezalı sorular için)</summary>
    public bool IsPenaltyApplied { get; set; }

    /// <summary>
    /// Uygulanan ceza tipi (CrmQualityPenaltyTypes.Ids)
    /// None=0, YellowCard=1, RedCard=2
    /// </summary>
    public int AppliedPenaltyTypeId { get; set; } = Enums.CrmQualityPenaltyTypes.Ids.None;

    /// <summary>"Gerekmedi" (N/A) olarak işaretlendi mi? (zorunlu olmayan sorular için)</summary>
    public bool IsNotApplicable { get; set; }

    // Navigation - Bu cevap için seçilen alt kriterler (puan kırılma nedenleri)
    public ICollection<CrmQualityAnswerSubCriteriaSelection> SubCriteriaSelections { get; set; } = new List<CrmQualityAnswerSubCriteriaSelection>();
}

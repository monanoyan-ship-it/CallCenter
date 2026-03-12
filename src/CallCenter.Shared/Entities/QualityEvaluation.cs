namespace CallCenter.Shared.Entities;

/// <summary>
/// Kalite Değerlendirmesi - Bir çağrı kaydının değerlendirilmesi.
/// SecretCustomer Evaluation entity'sinden adapte edildi.
/// Supervisor ses kaydını dinler ve checklist üzerinden puanlar.
/// </summary>
public class QualityEvaluation
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Hangi checklist ile değerlendirildi</summary>
    public int ChecklistId { get; set; }
    public QualityChecklist Checklist { get; set; } = null!;

    /// <summary>Değerlendirilen çağrı kaydı</summary>
    public int CallRecordId { get; set; }
    public CallRecord CallRecord { get; set; } = null!;

    /// <summary>Değerlendirmeyi yapan kişi (Supervisor/Admin)</summary>
    public int EvaluatorPersonnelId { get; set; }
    public CustomerPersonnel EvaluatorPersonnel { get; set; } = null!;

    /// <summary>Değerlendirilen agent</summary>
    public int? EvaluatedPersonnelId { get; set; }
    public CustomerPersonnel? EvaluatedPersonnel { get; set; }

    /// <summary>Hangi müşteriye ait (denormalize - hızlı filtreleme)</summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>
    /// Değerlendirme durumu (QualityEvaluationStatuses.Ids)
    /// Pending=1, InProgress=2, Completed=3, Draft=4, Cancelled=5
    /// </summary>
    public int StatusId { get; set; } = Enums.QualityEvaluationStatuses.Ids.Pending;

    /// <summary>Alınan toplam puan</summary>
    public decimal? TotalScore { get; set; }

    /// <summary>Alınabilecek maksimum puan</summary>
    public decimal? MaxScore { get; set; }

    /// <summary>Puan yüzdesi (0-100)</summary>
    public decimal? ScorePercentage { get; set; }

    /// <summary>Değerlendirmenin başladığı zaman</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Değerlendirmenin tamamlandığı zaman</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Genel değerlendirme yorumu</summary>
    public string? EvaluationComment { get; set; }

    /// <summary>Değerlendirici notları</summary>
    public string? Notes { get; set; }

    /// <summary>Sarı kart sayısı</summary>
    public int YellowCardCount { get; set; }

    /// <summary>Kırmızı kart sayısı</summary>
    public int RedCardCount { get; set; }

    /// <summary>Form açılma tarihi (değerlendirme süresi hesabı için)</summary>
    public DateTime? FormOpenedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<QualityAnswer> Answers { get; set; } = new List<QualityAnswer>();
}

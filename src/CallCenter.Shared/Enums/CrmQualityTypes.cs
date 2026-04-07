namespace CallCenter.Shared.Enums;

/// <summary>
/// Kalite değerlendirme puanlama yöntemleri.
/// SecretCustomer ScoringMethods'dan adapte edildi.
/// </summary>
public static class CrmQualityScoringMethods
{
    /// <summary>Maximum: (GivenPoints / MaxPoints) * WeightPoints — Klasik ağırlıklı puanlama</summary>
    public static readonly TypeItem Maximum = new(1, "Maximum", "CrmQualityScoringMethod.Maximum", "Maksimum Yöntem", "bi-calculator", "bg-primary", 1, isDefault: true);

    /// <summary>CriteriaTotal: Seçenek puanı direkt sayılır — Alt kriter bazlı puanlama</summary>
    public static readonly TypeItem CriteriaTotal = new(2, "CriteriaTotal", "CrmQualityScoringMethod.CriteriaTotal", "Kriter Toplam Yöntem", "bi-list-check", "bg-info", 2);

    public static IEnumerable<TypeItem> All => new[] { Maximum, CriteriaTotal };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Maximum = 1;
        public const int CriteriaTotal = 2;
    }
}

/// <summary>
/// Soru puanlama tipleri.
/// SecretCustomer ScoringTypes'dan adapte edildi.
/// </summary>
public static class CrmQualityScoringTypes
{
    /// <summary>Puanlı — Normal puanlama yapılır</summary>
    public static readonly TypeItem Scored = new(1, "Scored", "CrmQualityScoringType.Scored", "Puanlı", "bi-star-fill", "bg-success", 1, isDefault: true);

    /// <summary>Puansız — Sadece gözlem, puan hesaplanmaz</summary>
    public static readonly TypeItem Unscored = new(2, "Unscored", "CrmQualityScoringType.Unscored", "Puansız", "bi-eye", "bg-secondary", 2);

    /// <summary>Cezalı — Sarı/kırmızı kart uygulanır</summary>
    public static readonly TypeItem Penalty = new(3, "Penalty", "CrmQualityScoringType.Penalty", "Cezalı", "bi-exclamation-triangle-fill", "bg-danger", 3);

    public static IEnumerable<TypeItem> All => new[] { Scored, Unscored, Penalty };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Scored = 1;
        public const int Unscored = 2;
        public const int Penalty = 3;
    }
}

/// <summary>
/// Ceza tipleri (sarı kart / kırmızı kart).
/// SecretCustomer PenaltyTypes'dan adapte edildi.
/// </summary>
public static class CrmQualityPenaltyTypes
{
    public static readonly TypeItem None = new(0, "None", "CrmQualityPenaltyType.None", "Ceza Yok", "bi-dash", "bg-light text-dark", 0, isDefault: true);

    /// <summary>Sarı Kart — Uyarı seviyesi hata</summary>
    public static readonly TypeItem YellowCard = new(1, "YellowCard", "CrmQualityPenaltyType.YellowCard", "Sarı Kart", "bi-card-heading", "bg-warning text-dark", 1);

    /// <summary>Kırmızı Kart — Kritik hata (ör: müşteriye hakaret, yanlış bilgi verme)</summary>
    public static readonly TypeItem RedCard = new(2, "RedCard", "CrmQualityPenaltyType.RedCard", "Kırmızı Kart", "bi-card-heading", "bg-danger", 2);

    public static IEnumerable<TypeItem> All => new[] { None, YellowCard, RedCard };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int None = 0;
        public const int YellowCard = 1;
        public const int RedCard = 2;
    }
}

/// <summary>
/// Değerlendirme durumları.
/// SecretCustomer EvaluationStatuses'dan adapte edildi.
/// </summary>
public static class CrmQualityEvaluationStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", "CrmQualityEvaluationStatus.Pending", "Bekliyor", "bi-hourglass", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(2, "InProgress", "CrmQualityEvaluationStatus.InProgress", "Devam Ediyor", "bi-pencil-fill", "bg-primary", 2);
    public static readonly TypeItem Completed = new(3, "Completed", "CrmQualityEvaluationStatus.Completed", "Tamamlandı", "bi-check-circle-fill", "bg-success", 3);
    public static readonly TypeItem Draft = new(4, "Draft", "CrmQualityEvaluationStatus.Draft", "Taslak", "bi-file-earmark", "bg-info", 4);
    public static readonly TypeItem Cancelled = new(5, "Cancelled", "CrmQualityEvaluationStatus.Cancelled", "İptal Edildi", "bi-x-circle-fill", "bg-danger", 5);

    public static IEnumerable<TypeItem> All => new[] { Pending, InProgress, Completed, Draft, Cancelled };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Pending = 1;
        public const int InProgress = 2;
        public const int Completed = 3;
        public const int Draft = 4;
        public const int Cancelled = 5;
    }
}

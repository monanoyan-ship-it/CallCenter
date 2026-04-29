namespace CallCenter.Shared.Entities;

/// <summary>
/// Musteri aboneligi. Hangi plan ile ne zaman baslamis, sonraki tahakkuk ne zaman.
/// Tahakkuk olusturulurken bu kayda bakilir.
/// </summary>
public class CustomerSubscription
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;

    /// <summary>Hangi sube icin — null = tum firma (eski davranis)</summary>
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    /// <summary>Abonelik baslangic tarihi (ilk odeme tarihi)</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Aylik birim fiyat (modullerin toplam fiyati + urun fiyati)</summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>Donem fiyati (MonthlyPrice x IntervalMonths x (1 - DiscountPercent/100))</summary>
    public decimal PeriodPrice { get; set; }

    /// <summary>Tahakkuk gunu (ayin kaci). StartDate'in gunu.</summary>
    public int BillingDay { get; set; }

    /// <summary>Sonraki tahakkuk tarihi</summary>
    public DateTime NextBillingDate { get; set; }

    /// <summary>1=Aktif, 2=Askiya Alinmis, 3=Iptal</summary>
    public int StatusId { get; set; } = 1;

    /// <summary>Odeme icin ek gunler — ucretli suspend mantigi PeriodStartDate ile hizali kullanilir; varsayilan 5.</summary>
    public int PaymentGraceDays { get; set; } = 5;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
}

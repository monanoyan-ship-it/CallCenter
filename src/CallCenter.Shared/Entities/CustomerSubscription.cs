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

    /// <summary>Odeme icin son gun (tahakkuk + 7 gun)</summary>
    public int PaymentGraceDays { get; set; } = 7;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
}

using CallCenter.Shared.Entities;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISubscriptionFactory
{
    // Plan
    Task<List<SubscriptionPlan>> GetPlansAsync();
    Task<SubscriptionPlan> CreatePlanAsync(string name, int intervalMonths, decimal discountPercent);
    Task<(bool Success, string? Error)> UpdatePlanAsync(int id, string name, int intervalMonths, decimal discountPercent, bool isActive);
    Task<(bool Success, string? Error)> DeletePlanAsync(int id);

    // Abonelik
    Task<List<object>> GetCustomerSubscriptionsAsync(int? customerId = null);
    Task<(object? Result, string? Error)> CreateSubscriptionAsync(int customerId, int planId, DateTime startDate, decimal monthlyPrice, int? branchId = null, decimal? discountPercentOverride = null);
    Task<(bool Success, string? Error)> CancelSubscriptionAsync(int subscriptionId);

    /// <summary>Secilen yil/ay: sonraki kesim bu aya düşen <b>aktif abonelikler</b> için salon platform tahakkuku (Modüller / Abonelikler).</summary>
    Task<(int Created, int Skipped, int EligibleSubscriptions)> GenerateBillingForMonthAsync(int year, int month);

    /// <summary>
    /// Müşteriler / Toplu faturalama: <b>aktif aboneliği olmayan</b> Salon müşterileri için
    /// seçilen takvim ayına liste fiyatından aylık (1 ay) platform tahakkuku. BillingAnchorDay boşsa ilk kesimde tahakkuk günü atanır.
    /// </summary>
    Task<(int Created, int Skipped, int EligibleWithoutSubscription)> GenerateSalonPlatformBillingForMonthWithoutSubscriptionAsync(int year, int month);

    /// <summary>Salon kayit: ilk donem tahakkukunu olusturur (aylik job ile ayni mantik). Idempotent.</summary>
    Task CreateInitialBillingPeriodForCustomerAsync(int customerId);

    /// <summary>
    /// Yonetim: aktif Salon urunu, CC urunu OLMAYAN musteri icin manuel <see cref="CustomerBillingKinds.SalonPlatform"/> donemi.
    /// Tutarlar otomatik tahakkuk ile ayni hesaplanir; abonelik tarihleri degismez.
    /// </summary>
    Task<(bool Success, string? Error)> CreateManualSalonPlatformPeriodAsync(int customerId, DateTime periodStartDate, string? notes);

    // Musteri kendi abonelik durumu
    Task<object> GetMySubscriptionAsync(int customerId);

    /// <summary>Salon layout: ödenmemiş / gecikmiş platform tahakkuku (tahakkuk tarihi ve grace).</summary>
    Task<object> GetSalonBannerAsync(int customerId);

    /// <summary>Salon panel: ücretli abonelik veya aboneliksiz platform borcunda grace süresi içinde erişim; deneme için sonraki kesim+grace.</summary>
    Task<object> GetSalonPanelAccessAsync(int customerId);

    // BUG2.4: aktif abonelik var mi (null = yok / iptal / askida)
    Task<bool> HasActiveSubscriptionAsync(int customerId);

    /// <summary>
    /// Yonetim / musteri detayindaki <see cref="CustomerSubscription.MonthlyPrice"/> ozetini gunceller:
    /// aktif fiyat donemi (Core) + plan interval/indirim + sube + ek modul paketleri. Tahakkuku etkilemez.
    /// Deneme (PeriodPrice=0) dahil tum aktif aboneliklerde calisir.
    /// </summary>
    Task RefreshSubscriptionDisplayMonthlyPriceAsync(int customerId, bool saveChanges = true);

    /// <summary>
    /// Salon platform tahakkuk tutarlari (temel paket + ek subeler, ayri modul paketleri).
    /// Aktif <see cref="CustomerSubscription"/> yoksa null.
    /// </summary>
    Task<(decimal PlatformAmount, decimal ModuleAmount)?> GetSalonBillingBreakdownForCustomerAsync(int customerId);

    /// <summary>
    /// Kart ile platform ödemesi: en eski borç dönemi ve tutar.
    /// Ücretli aktif abonelik veya aboneliksiz platform tahakkuku (liste fiyatı kırılımı) için doldurulur; deneme (PeriodPrice=0) hariç.
    /// </summary>
    Task<(CustomerBillingPeriod Period, decimal PayAmount)?> TryResolveSalonSubscriptionPaymentAsync(int customerId);

    /// <summary>Aktif abonelik için bir sonraki platform tahakkuk başlangıç tarihi (UTC). Abonelik yoksa null.</summary>
    Task<DateTime?> GetNextSalonAccrualUtcAsync(int customerId);
}

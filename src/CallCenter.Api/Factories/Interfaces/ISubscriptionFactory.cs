using CallCenter.Shared.Entities;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISubscriptionFactory
{
    // Plan
    Task<List<SubscriptionPlan>> GetPlansAsync();
    Task<SubscriptionPlan> CreatePlanAsync(string name, int intervalMonths, decimal discountPercent, decimal branchPrice);
    Task<(bool Success, string? Error)> UpdatePlanAsync(int id, string name, int intervalMonths, decimal discountPercent, decimal branchPrice, bool isActive);
    Task<(bool Success, string? Error)> DeletePlanAsync(int id);

    // Abonelik
    Task<List<object>> GetCustomerSubscriptionsAsync(int? customerId = null);
    Task<(object? Result, string? Error)> CreateSubscriptionAsync(int customerId, int planId, DateTime startDate, decimal monthlyPrice, int? branchId = null);
    Task<(bool Success, string? Error)> CancelSubscriptionAsync(int subscriptionId);

    /// <summary>Secilen yil/ay + BillingDay: aktif plan abonelikleri icin platform tahakkuku. Odenmis gercek fatura haric varsa yeniden keser.</summary>
    Task<(int Created, int Skipped)> GenerateBillingForMonthAsync(int year, int month);

    /// <summary>Salon kayit: ilk donem tahakkukunu olusturur (aylik job ile ayni mantik). Idempotent.</summary>
    Task CreateInitialBillingPeriodForCustomerAsync(int customerId);

    // Musteri kendi abonelik durumu
    Task<object> GetMySubscriptionAsync(int customerId);

    /// <summary>Salon layout: ödenmemiş / gecikmiş platform tahakkuku (tahakkuk tarihi ve grace).</summary>
    Task<object> GetSalonBannerAsync(int customerId);

    /// <summary>Salon panel: aktif abonelik veya tahakkuk gecikme hakki ile erisim.</summary>
    Task<object> GetSalonPanelAccessAsync(int customerId);

    // BUG2.4: aktif abonelik var mi (null = yok / iptal / askida)
    Task<bool> HasActiveSubscriptionAsync(int customerId);

    /// <summary>
    /// Yonetim listesindeki aylik tutar: PeriodPrice + ek sube + aktif ek modul paketleri (donem toplaminin aylik ortalamasi).
    /// Tahakkuk hesabini degistirmez; modul/satir degisince cagirin.
    /// </summary>
    Task RefreshSubscriptionDisplayMonthlyPriceAsync(int customerId, bool saveChanges = true);
}

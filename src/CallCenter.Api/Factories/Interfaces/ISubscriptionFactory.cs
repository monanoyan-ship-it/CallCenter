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
    Task<(object? Result, string? Error)> CreateSubscriptionAsync(int customerId, int planId, DateTime startDate, decimal monthlyPrice);
    Task<(bool Success, string? Error)> CancelSubscriptionAsync(int subscriptionId);

    // Tahakkuk
    Task<(int Created, int Skipped)> GenerateBillingForMonthAsync(int year, int month);

    // Musteri kendi abonelik durumu
    Task<object> GetMySubscriptionAsync(int customerId);
}

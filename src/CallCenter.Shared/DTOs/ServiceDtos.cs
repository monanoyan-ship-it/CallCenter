using System.ComponentModel.DataAnnotations;

namespace CallCenter.Shared.DTOs;

// ─── ServiceType (TypeDefinition bazli, DB entity yok) ───

public class ServiceTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string BadgeCss { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

// ─── CustomerServiceSubscription ───

public class SubscriptionDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int CustomerId { get; set; }
    public int ServiceTypeId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
}

public class SubscriptionCreateDto
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int ServiceTypeId { get; set; }

    public decimal? MonthlyPrice { get; set; }
    public string? Notes { get; set; }
}

public class SubscriptionUpdateDto
{
    public int? StatusId { get; set; }
    public decimal? MonthlyPrice { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
}

// ─── ServiceBillingItem ───

public class ServiceBillingItemDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int SubscriptionId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}

// ─── Toplu Fiyat Artırma ───

public class BulkPriceAdjustDto
{
    [Required]
    public string AdjustmentType { get; set; } = string.Empty; // "amount" | "percent"

    [Required]
    public decimal Value { get; set; }

    public int? ServiceTypeId { get; set; }
}

public class BulkPriceAdjustResultDto
{
    public int AffectedCount { get; set; }
    public decimal OldTotalMonthly { get; set; }
    public decimal NewTotalMonthly { get; set; }
}

// ─── Dashboard / Özet ───

public class ServiceSubscriptionSummaryDto
{
    public int TotalActiveSubscriptions { get; set; }
    public decimal TotalMonthlyRevenue { get; set; }
    public List<ServiceRevenueItem> ServiceBreakdown { get; set; } = new();
}

public class ServiceRevenueItem
{
    public string ServiceName { get; set; } = string.Empty;
    public int ActiveCount { get; set; }
    public decimal TotalMonthly { get; set; }
}

using CallCenter.Shared.Enums;

namespace CallCenter.Shared.Entities;

public class CustomerBillingPeriod
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary><see cref="CustomerBillingKinds"/> — CC, salon platform, CRM vb.</summary>
    public int BillingKindId { get; set; } = CustomerBillingKinds.CallCenter;

    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public int UserCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Hizmet abonelikleri toplam tutari</summary>
    public decimal ServiceAmount { get; set; }
    public int StatusId { get; set; } = 1; // BillingPeriodStatuses
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public int? PaymentMethodId { get; set; } // BillingPaymentMethods
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Salon portal modulleri — tahakkuk anindaki kalemler.</summary>
    public ICollection<CustomerBillingPeriodModuleLine> ModuleLines { get; set; } = [];
}

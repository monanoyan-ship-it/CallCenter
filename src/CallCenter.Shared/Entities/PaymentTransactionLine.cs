namespace CallCenter.Shared.Entities;

/// <summary>
/// Tek odeme altinda kapatilan tahakkuk kalemi.
/// </summary>
public class PaymentTransactionLine
{
    public int Id { get; set; }
    public int PaymentTransactionId { get; set; }
    public PaymentTransaction PaymentTransaction { get; set; } = null!;

    public int? BillingPeriodId { get; set; }
    public CustomerBillingPeriod? BillingPeriod { get; set; }

    public int? BillingKindId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

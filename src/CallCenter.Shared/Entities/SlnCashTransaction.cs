namespace CallCenter.Shared.Entities;

public class SlnCashTransaction
{
    public int Id { get; set; }
    public int RegisterId { get; set; }
    public SlnCashRegister? Register { get; set; }

    /// <summary>1=Income, 2=Expense, 3=Transfer</summary>
    public int TransactionTypeId { get; set; }

    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>1=Cash, 2=CreditCard</summary>
    public int PaymentMethodId { get; set; } = 1;

    public int? RelatedInvoiceId { get; set; }
    public SlnInvoice? RelatedInvoice { get; set; }

    public int? CreatedByPersonnelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

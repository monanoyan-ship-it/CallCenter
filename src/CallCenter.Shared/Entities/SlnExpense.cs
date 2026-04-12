namespace CallCenter.Shared.Entities;

public class SlnExpense
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Hangi sube (null = merkez)</summary>
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public int CategoryId { get; set; }
    public SlnExpenseCategory? Category { get; set; }

    public decimal Amount { get; set; }
    /// <summary>KDV tutari</summary>
    public decimal TaxAmount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }

    /// <summary>1=Beklemede, 2=Onayli, 3=Reddedildi</summary>
    public int StatusId { get; set; } = 2;

    /// <summary>Onaylayan personel</summary>
    public int? ApprovedByPersonnelId { get; set; }

    /// <summary>Belge/fatura referansi</summary>
    public string? DocumentRef { get; set; }

    /// <summary>1=Cash, 2=CreditCard, 3=BankTransfer</summary>
    public int PaymentMethodId { get; set; } = 1;

    public int? CreatedByPersonnelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

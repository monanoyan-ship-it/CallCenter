namespace CallCenter.Shared.Entities;

public class SlnExpense
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int CategoryId { get; set; }
    public SlnExpenseCategory? Category { get; set; }

    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }

    /// <summary>1=Cash, 2=CreditCard, 3=BankTransfer</summary>
    public int PaymentMethodId { get; set; } = 1;

    public int? CreatedByPersonnelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

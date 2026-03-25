namespace CallCenter.Shared.Entities;

public class SlnExpenseCategory
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }

    public ICollection<SlnExpense> Expenses { get; set; } = [];
}

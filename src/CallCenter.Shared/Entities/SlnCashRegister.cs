namespace CallCenter.Shared.Entities;

public class SlnCashRegister
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Hangi sube (null = merkez)</summary>
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<SlnCashTransaction> Transactions { get; set; } = [];
}

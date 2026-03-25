namespace CallCenter.Shared.Entities;

public class SlnPosDevice
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int BankAccountId { get; set; }
    public SlnBankAccount? BankAccount { get; set; }

    public string DeviceName { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public bool IsActive { get; set; } = true;
}

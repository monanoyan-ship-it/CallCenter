namespace CallCenter.Shared.Entities;

public class SlnBankAccount
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string BankName { get; set; } = string.Empty;
    public string? AccountNo { get; set; }
    public string? IBAN { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SlnPosDevice> PosDevices { get; set; } = [];
}

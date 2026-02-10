namespace CallCenter.Shared.Entities;

public class SipAccount
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 5060;
    public string? Domain { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Transport { get; set; } = "UDP";
    public bool UseSrtp { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Hangi musteriye ait (her SIP hesabi bir firmaya baglidir)
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Opsiyonel organizasyon birimi baglantisi
    public int? OrganizationUnitId { get; set; }
    public CustomerOrganizationUnit? OrganizationUnit { get; set; }
}

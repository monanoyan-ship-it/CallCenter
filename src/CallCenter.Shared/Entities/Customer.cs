namespace CallCenter.Shared.Entities;

public class Customer
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxUsers { get; set; } = 1;
    public decimal MonthlyUnitPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CustomerPersonnel> Personnel { get; set; } = new List<CustomerPersonnel>();

    // Müşteriye açık portal modülleri
    public ICollection<CustomerPortalModule> PortalModules { get; set; } = new List<CustomerPortalModule>();

    // Musteriye ait kuyruklar
    public ICollection<Queue> Queues { get; set; } = new List<Queue>();

    // Musteriye ait SIP hesaplari
    public ICollection<SipAccount> SipAccounts { get; set; } = new List<SipAccount>();

    // Musteriye ait organizasyon birimleri
    public ICollection<CustomerOrganizationUnit> OrganizationUnits { get; set; } = new List<CustomerOrganizationUnit>();

    // Musteriye ait bulut depolama yapilandirmalari
    public ICollection<CustomerStorageConfig> StorageConfigs { get; set; } = new List<CustomerStorageConfig>();

    // Faturalama donemleri
    public ICollection<CustomerBillingPeriod> BillingPeriods { get; set; } = new List<CustomerBillingPeriod>();

    // Hizmet abonelikleri
    public ICollection<CustomerServiceSubscription> ServiceSubscriptions { get; set; } = new List<CustomerServiceSubscription>();
}

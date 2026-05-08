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

    /// <summary>Salon/firma timezone (IANA format: Europe/Istanbul, Europe/Berlin vb.)</summary>
    public string TimeZone { get; set; } = "Europe/Istanbul";

    /// <summary>Test/beta musteri — tahakkuk olusturulmaz, moduller ucretsiz</summary>
    public bool IsTest { get; set; }

    /// <summary>Test musterisi notu (neden test, ne zaman bitiyor vb.)</summary>
    public string? TestNotes { get; set; }

    public int MaxUsers { get; set; } = 1;
    public bool SaveRecordingToPlatform { get; set; } = true;
    public bool SaveRecordingToOwnStorage { get; set; }
    public bool AutoRecordCalls { get; set; }
    public bool IsCallbackManagementEnabled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ─── Pazaryeri Komisyon Yapilandirmasi (PS.8) ───
    /// <summary>Corplynk araciligiyla yapilan tahsilatlardan kesilen komisyon yuzdesi (default %5).</summary>
    public decimal MarketplaceCommissionPercent { get; set; } = 5m;
    /// <summary>%1 e-ticaret aracilik stopaji veya benzeri firma-bazli vergi (default 0).</summary>
    public decimal MarketplaceWithholdingPercent { get; set; }

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

    // Musteri urunleri (CallCenter, Salon vb.)
    public ICollection<CustomerProduct> Products { get; set; } = new List<CustomerProduct>();

    // Modul talepleri
    public ICollection<ModuleRequest> ModuleRequests { get; set; } = new List<ModuleRequest>();

    /// <summary>Tahakkuk gunu (ayin kaci). null = ilk tahakkuk henuz olusturulmadi</summary>
    public int? BillingAnchorDay { get; set; }

    /// <summary>Faturalama tipi: 1=Tek fatura (merkez sube bilgileri), 2=Sube bazli ayri fatura</summary>
    public int BillingType { get; set; } = 1;

    // Subeler
    public ICollection<SlnBranch> Branches { get; set; } = new List<SlnBranch>();

    // Abonelik
    public ICollection<CustomerSubscription> Subscriptions { get; set; } = new List<CustomerSubscription>();
}

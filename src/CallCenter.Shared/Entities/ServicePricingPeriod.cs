namespace CallCenter.Shared.Entities;

/// <summary>
/// Hizmet fiyatlandirma donemi. Her donem icin hizmet fiyatlari belirlenir.
/// Yeni donem olustururken onceki donemin fiyatlari kopyalanir.
/// </summary>
public class ServicePricingPeriod
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>1=Aktif, 2=Gecmis, 3=Taslak</summary>
    public int StatusId { get; set; } = 3;

    /// <summary>Şube satırı artık dönem Temel Paket fiyatından hesaplanır; kolon veritabanında kalır, uygulama kullanmaz.</summary>
    public decimal ExtraBranchMonthlyPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ServicePricingItem> Items { get; set; } = [];

    public ICollection<ServicePricingBranchDiscountTier> BranchDiscountTiers { get; set; } = [];
}

/// <summary>
/// Donem icindeki hizmet fiyat kalemi.
/// ProductTypeId ile CC/Salon ayrimi yapilir.
/// ServiceId: CC icin ServiceTypes.Ids, Salon icin SalonPortalModules.Ids
/// PackageGroupId doluysa kalem bir paket fiyatini temsil eder (ServiceId=0 olabilir).
/// </summary>
public class ServicePricingItem
{
    public int Id { get; set; }

    public int PeriodId { get; set; }
    public ServicePricingPeriod? Period { get; set; }

    /// <summary>1=CallCenter, 2=Salon</summary>
    public int ProductTypeId { get; set; }

    /// <summary>Hizmet/modul ID (CC: ServiceTypes.Ids, Salon: SalonPortalModules.Ids). Paket satirinda 0.</summary>
    public int ServiceId { get; set; }

    /// <summary>
    /// Paket satırı ise doludur (SalonModuleGroups.Ids). Null ise bu bir modül/hizmet fiyat kalemi.
    /// </summary>
    public int? PackageGroupId { get; set; }

    /// <summary>Hizmet adi (TypeDefinition dan kopyalanir)</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Aylik fiyat</summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>Onceki donem fiyati (karsilastirma icin)</summary>
    public decimal? PreviousPrice { get; set; }
}

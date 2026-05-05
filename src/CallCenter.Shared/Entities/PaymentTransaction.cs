namespace CallCenter.Shared.Entities;

/// <summary>
/// Online odeme islemi kaydi.
/// Hem salon adisyon odemesi hem platform abonelik odemesi icin kullanilir.
/// </summary>
public class PaymentTransaction
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Odeme tipi: 1=SalonAdisyon, 2=PlatformAbonelik, 3=UyelikOdemesi, 4=ModulSatinAlma</summary>
    public int PaymentTypeId { get; set; }

    /// <summary>Ilgili firma (salon veya CC musterisi)</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Platform kullanicisi (salon adisyon odemesinde)</summary>
    public int? PlatformUserId { get; set; }
    public PlatformUser? PlatformUser { get; set; }

    /// <summary>Ilgili adisyon (salon odemesinde)</summary>
    public int? InvoiceId { get; set; }

    /// <summary>Ilgili faturalama donemi (platform abonelikte)</summary>
    public int? BillingPeriodId { get; set; }

    /// <summary>Satin alinan modul ID (modul satin almada)</summary>
    public int? ModuleId { get; set; }

    /// <summary>Odeme tutari</summary>
    public decimal Amount { get; set; }

    /// <summary>Para birimi (TRY, USD, EUR)</summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>Odeme yontemi: 1=KrediKarti, 2=Havale, 3=MailOrder</summary>
    public int PaymentMethodId { get; set; }

    /// <summary>Odeme durumu: 1=Beklemede, 2=Basarili, 3=Basarisiz, 4=Iade, 5=Iptal</summary>
    public int StatusId { get; set; } = 1;

    /// <summary>Odeme saglayici (Iyzico, Stripe vb.)</summary>
    public string? Provider { get; set; }

    /// <summary>Saglayicidan donen islem ID</summary>
    public string? ProviderTransactionId { get; set; }

    /// <summary>Saglayicidan donen odeme ID</summary>
    public string? ProviderPaymentId { get; set; }

    /// <summary>Hata mesaji (basarisiz odemelerde)</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Kart son 4 hane (maskelenmis)</summary>
    public string? CardLastFour { get; set; }

    /// <summary>Taksit sayisi (0=tek cekim)</summary>
    public int InstallmentCount { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

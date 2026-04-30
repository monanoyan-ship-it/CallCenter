namespace CallCenter.Shared.Enums;

/// <summary>
/// Tahakkuk satırının ürün hattı — aynı müşteri ve takvim ayında birden çok tür olabilir.
/// </summary>
public static class CustomerBillingKinds
{
    public const int CallCenter = 1;
    public const int SalonPlatform = 2;
    public const int Crm = 3;

    public static string GetDescription(int id) => id switch
    {
        CallCenter => "Call Center",
        SalonPlatform => "Salon platform",
        Crm => "CRM",
        _ => $"Tür #{id}"
    };
}

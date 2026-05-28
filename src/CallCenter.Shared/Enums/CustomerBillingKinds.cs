namespace CallCenter.Shared.Enums;

/// <summary>
/// Tahakkuk satırının ürün hattı — aynı müşteri ve takvim ayında birden çok tür olabilir.
/// </summary>
public static class CustomerBillingKinds
{
    public const int CallCenter = 1;
    public const int SalonPlatform = 2;
    public const int Crm = 3;
    public const int SalonCrm = 4;
    public const int CallCenterCrm = 5;

    public static IEnumerable<int> CrmKinds => new[] { Crm, SalonCrm, CallCenterCrm };
    public static bool IsCrmKind(int id) => id == Crm || id == SalonCrm || id == CallCenterCrm;

    public static string GetDescription(int id) => id switch
    {
        CallCenter => "Call Center",
        SalonPlatform => "Salon platform",
        Crm => "Genel CRM",
        SalonCrm => "Salon CRM",
        CallCenterCrm => "CallCenter CRM",
        _ => $"Tür #{id}"
    };
}

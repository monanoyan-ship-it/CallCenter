namespace CallCenter.Shared.Enums;

/// <summary>
/// Platform hizmet katalogu. TypeDefinition olarak tanimlandi (DB entity yok).
/// CustomerServiceSubscription.ServiceTypeId bu ID'lere referans verir (FK yok).
/// </summary>
public static class ServiceTypes
{
    // ─── Dahil (Default) Hizmetler ───
    public static readonly TypeItem CagriDagitimi = new(1, "CagriDagitimi", "Service.CagriDagitimi", "Çağrı Dağıtımı (ACD)", "bi-telephone-forward-fill", "bg-success", 1, isDefault: true);
    public static readonly TypeItem CagriKaydi = new(2, "CagriKaydi", "Service.CagriKaydi", "Çağrı Kaydı (CDR)", "bi-journal-text", "bg-success", 2, isDefault: true);
    public static readonly TypeItem SesKayitlari = new(3, "SesKayitlari", "Service.SesKayitlari", "Ses Kayıtları", "bi-record-circle", "bg-success", 3, isDefault: true);

    // ─── Premium (Ucretli) Hizmetler ───
    public static readonly TypeItem IVR = new(4, "IVR", "Service.IVR", "Sesli Yönlendirme (IVR)", "bi-telephone-inbound-fill", "bg-warning text-dark", 4);
    public static readonly TypeItem KaliteYonetimi = new(5, "KaliteYonetimi", "Service.KaliteYonetimi", "Kalite Değerlendirme", "bi-clipboard-check", "bg-warning text-dark", 5);
    public static readonly TypeItem CRM = new(6, "CRM", "Service.CRM", "CRM Modülü (Müşteri Kartı, Ticket, Pipeline)", "bi-person-lines-fill", "bg-warning text-dark", 6);
    public static readonly TypeItem KampanyaModulu = new(7, "KampanyaModulu", "Service.KampanyaModulu", "Kampanya Modülü", "bi-megaphone-fill", "bg-success", 7, isDefault: true);
    public static readonly TypeItem GelismisRaporlama = new(8, "GelismisRaporlama", "Service.GelismisRaporlama", "Gelişmiş Raporlama", "bi-file-earmark-bar-graph", "bg-success", 8, isDefault: true);
    public static readonly TypeItem SMSMesajlasma = new(9, "SMSMesajlasma", "Service.SMSMesajlasma", "SMS / Mesajlaşma", "bi-chat-dots-fill", "bg-warning text-dark", 9);
    public static readonly TypeItem APIErisimi = new(10, "APIErisimi", "Service.APIErisimi", "API Erişimi", "bi-braces", "bg-warning text-dark", 10);
    public static readonly TypeItem BulutDepolama = new(11, "BulutDepolama", "Service.BulutDepolama", "Bulut Depolama", "bi-cloud-upload", "bg-success", 11, isDefault: true);
    public static readonly TypeItem EkSipHat = new(12, "EkSipHat", "Service.EkSipHat", "Ek SIP Hat", "bi-router-fill", "bg-warning text-dark", 12);
    public static readonly TypeItem KVKKPaketi = new(14, "KVKKPaketi", "Service.KVKKPaketi", "KVKK Uyumluluk Paketi", "bi-shield-check", "bg-success", 14, isDefault: true);
    public static readonly TypeItem OncelikliDestek = new(15, "OncelikliDestek", "Service.OncelikliDestek", "7/24 Öncelikli Destek", "bi-headset", "bg-warning text-dark", 15);
    public static readonly TypeItem SesliKarsilama = new(16, "SesliKarsilama", "Service.SesliKarsilama", "Sesli Karşılama (Auto-Attendant)", "bi-soundwave", "bg-warning text-dark", 16);
    public static IEnumerable<TypeItem> All => new[]
    {
        CagriDagitimi, CagriKaydi, SesKayitlari,
        IVR, KaliteYonetimi, CRM, KampanyaModulu, GelismisRaporlama,
        SMSMesajlasma, APIErisimi, BulutDepolama, EkSipHat, KVKKPaketi, OncelikliDestek,
        SesliKarsilama
    };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    /// <summary>Yeni musteri olusturuldiginda varsayilan olarak atanacak hizmetler</summary>
    public static IEnumerable<TypeItem> Defaults => All.Where(x => x.IsDefault);

    /// <summary>Premium (ucretli) hizmetler</summary>
    public static IEnumerable<TypeItem> Premium => All.Where(x => !x.IsDefault);

    /// <summary>Hizmetin kategori ID'si (1=Default, 2=Premium)</summary>
    public static int GetCategoryId(int id) => GetById(id)?.IsDefault == true
        ? ServiceCategories.Ids.Default
        : ServiceCategories.Ids.Premium;

    public static class Ids
    {
        public const int CagriDagitimi = 1;
        public const int CagriKaydi = 2;
        public const int SesKayitlari = 3;
        public const int IVR = 4;
        public const int KaliteYonetimi = 5;
        public const int CRM = 6;
        public const int KampanyaModulu = 7;
        public const int GelismisRaporlama = 8;
        public const int SMSMesajlasma = 9;
        public const int APIErisimi = 10;
        public const int BulutDepolama = 11;
        public const int EkSipHat = 12;
        public const int KVKKPaketi = 14;
        public const int OncelikliDestek = 15;
        public const int SesliKarsilama = 16;
    }
}

public static class ServiceCategories
{
    public static readonly TypeItem Default = new(1, "Default", "ServiceCategory.Default", "Standart Dahil Hizmet", "bi-check-circle-fill", "bg-success", 1, isDefault: true);
    public static readonly TypeItem Premium = new(2, "Premium", "ServiceCategory.Premium", "Ücretli Ek Hizmet", "bi-star-fill", "bg-warning text-dark", 2);

    public static IEnumerable<TypeItem> All => new[] { Default, Premium };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Default = 1;
        public const int Premium = 2;
    }
}

public static class SubscriptionStatuses
{
    public static readonly TypeItem Active = new(1, "Active", "SubscriptionStatus.Active", "Aktif Abonelik", "bi-check-circle-fill", "bg-success", 1, isDefault: true);
    public static readonly TypeItem Suspended = new(2, "Suspended", "SubscriptionStatus.Suspended", "Askıya Alınmış", "bi-pause-circle-fill", "bg-warning text-dark", 2);
    public static readonly TypeItem Cancelled = new(3, "Cancelled", "SubscriptionStatus.Cancelled", "İptal Edilmiş", "bi-x-circle-fill", "bg-danger", 3);

    public static IEnumerable<TypeItem> All => new[] { Active, Suspended, Cancelled };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Active = 1;
        public const int Suspended = 2;
        public const int Cancelled = 3;
    }
}

/// <summary>Platform tahakkuku ile salon panel erisimi — tek gecikme politikasi (gun).</summary>
public static class PlatformBillingAccessPolicy
{
    /// <summary>Yeni salon kaydindan itibaren ucretsiz demo suresi.</summary>
    public const int RegistrationTrialDays = 5;

    /// <summary>Tahakkuk kesiminden (PeriodStartDate) sonra odeme icin panel acik kalir; sonrasinda kilit.</summary>
    public const int UnpaidGraceDaysAfterPeriodStart = 5;

    /// <summary>Demo bitince ek grace verilmez; odeme grace'i yalniz tahakkuklu ucretli donemler icindir.</summary>
    public const int TrialSuspensionGraceDays = 0;
}

public static class BillingPeriodStatuses
{
    public static readonly TypeItem Draft = new(1, "Draft", "BillingPeriodStatus.Draft", "Tahakkuk", "bi-pencil-fill", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem Invoiced = new(2, "Invoiced", "BillingPeriodStatus.Invoiced", "Faturalanmış", "bi-receipt", "bg-info", 2);
    public static readonly TypeItem Paid = new(3, "Paid", "BillingPeriodStatus.Paid", "Ödenmiş", "bi-check-circle-fill", "bg-success", 3);
    public static readonly TypeItem Overdue = new(4, "Overdue", "BillingPeriodStatus.Overdue", "Gecikmiş", "bi-exclamation-triangle-fill", "bg-danger", 4);

    public static IEnumerable<TypeItem> All => new[] { Draft, Invoiced, Paid, Overdue };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Draft = 1;
        public const int Invoiced = 2;
        public const int Paid = 3;
        public const int Overdue = 4;
    }
}

public static class BillingPaymentMethods
{
    public static readonly TypeItem Havale = new(1, "Havale", "BillingPaymentMethod.Havale", "Havale/EFT", "bi-bank", "bg-primary", 1);
    public static readonly TypeItem MailOrder = new(2, "MailOrder", "BillingPaymentMethod.MailOrder", "Mail Order", "bi-credit-card-2-front", "bg-info", 2);
    public static readonly TypeItem KrediKarti = new(3, "KrediKarti", "BillingPaymentMethod.KrediKarti", "Kredi Kartı", "bi-credit-card", "bg-success", 3);

    public static IEnumerable<TypeItem> All => new[] { Havale, MailOrder, KrediKarti };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Havale = 1;
        public const int MailOrder = 2;
        public const int KrediKarti = 3;
    }
}

public static class BillingItemStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", "BillingItemStatus.Pending", "Beklemede", "bi-clock-fill", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Invoiced = new(2, "Invoiced", "BillingItemStatus.Invoiced", "Faturalanmış", "bi-receipt", "bg-info", 2);
    public static readonly TypeItem Paid = new(3, "Paid", "BillingItemStatus.Paid", "Ödenmiş", "bi-check-circle-fill", "bg-success", 3);
    public static readonly TypeItem Overdue = new(4, "Overdue", "BillingItemStatus.Overdue", "Gecikmiş", "bi-exclamation-triangle-fill", "bg-danger", 4);

    public static IEnumerable<TypeItem> All => new[] { Pending, Invoiced, Paid, Overdue };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Pending = 1;
        public const int Invoiced = 2;
        public const int Paid = 3;
        public const int Overdue = 4;
    }
}

// ─── Online Odeme Gateway Tanimlari ───

public static class PaymentProviders
{
    public static readonly TypeItem Iyzico = new(1, "Iyzico", "PaymentProvider.Iyzico", "Iyzico", "bi-credit-card-2-front", "bg-success", 1);
    public static readonly TypeItem PayTR = new(2, "PayTR", "PaymentProvider.PayTR", "PayTR", "bi-credit-card", "bg-primary", 2);
    public static readonly TypeItem Param = new(3, "Param", "PaymentProvider.Param", "Param", "bi-credit-card-fill", "bg-info", 3);

    public static IEnumerable<TypeItem> All => new[] { Iyzico, PayTR, Param };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Iyzico = 1;
        public const int PayTR = 2;
        public const int Param = 3;
    }
}

public static class PaymentTypes
{
    public static readonly TypeItem SalonAdisyon = new(1, "SalonAdisyon", "PaymentType.SalonAdisyon", "Salon Adisyon Ödemesi", "bi-receipt", "bg-info", 1);
    public static readonly TypeItem PlatformAbonelik = new(2, "PlatformAbonelik", "PaymentType.PlatformAbonelik", "Platform Abonelik Ödemesi", "bi-box-seam", "bg-primary", 2);
    public static readonly TypeItem UyelikOdemesi = new(3, "UyelikOdemesi", "PaymentType.UyelikOdemesi", "Üyelik Ödemesi", "bi-person-check", "bg-success", 3);
    public static readonly TypeItem ModulSatinAlma = new(4, "ModulSatinAlma", "PaymentType.ModulSatinAlma", "Modül Satın Alma", "bi-cart-check", "bg-warning text-dark", 4);
    public static readonly TypeItem RandevuOnOdemesi = new(5, "RandevuOnOdemesi", "PaymentType.RandevuOnOdemesi", "Randevu Ön Ödemesi/Depozito", "bi-calendar-check", "bg-info", 5);

    public static IEnumerable<TypeItem> All => new[] { SalonAdisyon, PlatformAbonelik, UyelikOdemesi, ModulSatinAlma, RandevuOnOdemesi };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int SalonAdisyon = 1;
        public const int PlatformAbonelik = 2;
        public const int UyelikOdemesi = 3;
        public const int ModulSatinAlma = 4;
        public const int RandevuOnOdemesi = 5;
    }
}

public static class PaymentStatuses
{
    public static readonly TypeItem Beklemede = new(1, "Beklemede", "PaymentStatus.Beklemede", "Beklemede", "bi-clock-fill", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Basarili = new(2, "Basarili", "PaymentStatus.Basarili", "Başarılı", "bi-check-circle-fill", "bg-success", 2);
    public static readonly TypeItem Basarisiz = new(3, "Basarisiz", "PaymentStatus.Basarisiz", "Basarisiz", "bi-x-circle-fill", "bg-danger", 3);
    public static readonly TypeItem Iade = new(4, "Iade", "PaymentStatus.Iade", "Iade Edildi", "bi-arrow-counterclockwise", "bg-warning text-dark", 4);
    public static readonly TypeItem Iptal = new(5, "Iptal", "PaymentStatus.Iptal", "Iptal Edildi", "bi-slash-circle", "bg-secondary", 5);

    public static IEnumerable<TypeItem> All => new[] { Beklemede, Basarili, Basarisiz, Iade, Iptal };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Beklemede = 1;
        public const int Basarili = 2;
        public const int Basarisiz = 3;
        public const int Iade = 4;
        public const int Iptal = 5;
    }
}

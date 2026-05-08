namespace CallCenter.Shared.Enums;

/// <summary>
/// Salon rol bazli sayfa erisim izinleri.
///
/// KULLANIM: Bir rolu duzenlemek icin asagidaki diziye sayfa ekle/cikar.
/// Controller isimleri _Layout.cshtml deki ctrl degerleriyle eslesir.
///
/// ROLLER:
///   101 = Salon Sahibi  (her seye erisir)
///   102 = Mudur          (yonetim sayfalari haric her sey)
///   103 = Kuafor         (kendi isleri + satis)
///   104 = Guzellik Uzmani (kendi isleri + satis)
///   105 = Kasiyer        (satis + finans + musteri)
///   106 = Resepsiyonist  (randevu + musteri)
/// </summary>
public static class SalonRolePermissions
{
    // ═══════════════════════════════════════════════════════════════
    //  HERKES - Tum roller bu sayfalara erisebilir
    // ═══════════════════════════════════════════════════════════════
    private static readonly string[] EveryonePages = new[]
    {
        "Home",         // Dashboard
        "Sales",        // Hizli Satis POS
    };

    // ═══════════════════════════════════════════════════════════════
    //  ROL BAZLI EK SAYFALAR
    //  (EveryonePages + asagidaki sayfalar = o rolun toplam erisimi)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>106 - Resepsiyonist: Randevu ve musteri islemleri</summary>
    private static readonly string[] ReceptionistPages = new[]
    {
        "Clients",          // Musteri listesi
        "Appointments",     // Randevu takvimi
        "Waitlist",         // Bekleme listesi
    };

    /// <summary>105 - Kasiyer: Satis ve finans islemleri</summary>
    private static readonly string[] CashierPages = new[]
    {
        "Invoices",         // Adisyonlar
        "Cash",             // Kasa
        "GiftCards",        // Hediye kartlari
        "Packages",         // Seans paketleri
    };

    /// <summary>103/104 - Kuafor/Uzman: Kendi hizmetleri</summary>
    private static readonly string[] TechnicianPages = new[]
    {
        "BeforeAfter",      // Once/sonra fotograflari
        "Recipes",          // Receteler (goruntuleme)
    };

    /// <summary>102 - Mudur: Operasyon + pazarlama + raporlama</summary>
    private static readonly string[] ManagerPages = new[]
    {
        "Services",         // Hizmet tanimlari
        "Recipes",          // Receteler (duzenleme)
        "Staff",            // Personel (goruntuleme)
        "PersonnelPrices",  // Personel fiyatlari
        "Products",         // Urunler
        "Suppliers",        // Tedarikciler
        "Expenses",         // Masraflar
        "Campaigns",        // SMS kampanyalari
        "EmailCampaigns",   // E-posta kampanyalari
        "Memberships",      // Uyelik planlari
        "Loyalty",          // Sadakat programi
        "Reviews",          // Yorum yonetimi
        "Winback",          // Kayip musteri
        "Reports",          // Raporlar
    };

    /// <summary>101 - Salon Sahibi: Sadece sahibine ozel sayfalar</summary>
    private static readonly string[] OwnerOnlyPages = new[]
    {
        "Profile",          // Salon profili
        "Branches",         // Sube yonetimi
        "NoShowPolicy",     // No-show politikasi
        "ConsentForms",     // Onay formlari
        "EmailSettings",    // E-posta entegrasyonu
        "PageSettings",     // Public sayfa ayarlari
        "Modules",          // Modul yonetimi ve talep
        "PaymentInfo",      // iyzico Pazaryeri sub-merchant onboarding (PS.5)
    };

    // ═══════════════════════════════════════════════════════════════
    //  ANA METOD - Rol ID'sine gore erisim kontrolu
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Belirtilen rolun belirtilen sayfaya erisip erisemeyecegini dondurur.
    /// </summary>
    public static bool CanAccess(int roleId, string controllerName)
    {
        // Salon sahibi her sayfaya erisebilir — yeni sayfa eklendiginde listeye ekleme zorunlulugu yok
        if (roleId == SalonRoles.Ids.SalonOwner)
            return true;

        return GetAccessiblePages(roleId).Contains(controllerName);
    }

    /// <summary>
    /// Belirtilen rolun erisebilecegi tum sayfa (controller) isimlerini dondurur.
    /// Kullanim: Layout sidebar'da menu filtreleme.
    /// </summary>
    public static HashSet<string> GetAccessiblePages(int roleId)
    {
        var pages = new HashSet<string>(EveryonePages, StringComparer.OrdinalIgnoreCase);

        // Hiyerarsik: ust rol alt rolun sayfalarini da icerir
        switch (roleId)
        {
            case SalonRoles.Ids.SalonOwner: // 101 - Her sey
                pages.UnionWith(OwnerOnlyPages);
                pages.UnionWith(ManagerPages);
                pages.UnionWith(CashierPages);
                pages.UnionWith(ReceptionistPages);
                pages.UnionWith(TechnicianPages);
                break;

            case SalonRoles.Ids.Manager: // 102 - Yonetim haric her sey
                pages.UnionWith(ManagerPages);
                pages.UnionWith(CashierPages);
                pages.UnionWith(ReceptionistPages);
                pages.UnionWith(TechnicianPages);
                break;

            case SalonRoles.Ids.BranchManager: // 107 - Sube Muduru: kendi subesi icin operasyon + finans
                                                // YONETIM (Branches, Profile, Modules, NoShowPolicy, EmailSettings, PageSettings) YASAK.
                                                // BranchId JWT claim i API tarafinda query filtrelemesini zorlar.
                pages.UnionWith(ManagerPages);
                pages.UnionWith(CashierPages);
                pages.UnionWith(ReceptionistPages);
                pages.UnionWith(TechnicianPages);
                break;

            case SalonRoles.Ids.Cashier: // 105 - Satis + finans + musteri + randevu
                pages.UnionWith(CashierPages);
                pages.UnionWith(ReceptionistPages);
                break;

            case SalonRoles.Ids.Receptionist: // 106 - Randevu + musteri
                pages.UnionWith(ReceptionistPages);
                break;

            case SalonRoles.Ids.Hairdresser: // 103
            case SalonRoles.Ids.Beautician:  // 104
                pages.UnionWith(TechnicianPages);
                pages.UnionWith(ReceptionistPages); // Randevularini gorsun
                pages.UnionWith(CashierPages);      // POS/adisyon yapabilsin
                break;
        }

        return pages;
    }
}

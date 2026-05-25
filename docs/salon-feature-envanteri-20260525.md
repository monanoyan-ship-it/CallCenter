# Salon Feature Envanteri

Tarih: 2026-05-25

Bu envanter `SALONUXGAP.1` icin hazirlandi. Kaynak olarak Salon MVC controller/view/js dosyalari, API factory/controller yuzeyi, `AppDbContext` Sln entity listesi ve son UX commitleri kullanildi. Runtime smoke bilerek sabaha birakildi; bu dosya kod ve ekran yuzeyi envanteridir.

## Durum Anahtari

| Durum | Anlam |
|---|---|
| Var | Kullaniciya acik ekran/API/entity akisi var. |
| Kismi | Ana parca var ama otomasyon, raporlama veya UX tamamlayici eksik. |
| Altyapi var | Entity/API parcasi var, owner icin tam is akisi degil. |
| Yok | Mevcut urunde dogrudan karsiligi yok. |

## Matris

| Alan | Durum | Kanit | Not |
|---|---|---|---|
| Salon kayit, login, e-posta dogrulama, sifre reset | Var | `AccountController`, `PublicSalonController`, auth cookie/JWT akisi | Owner ve public musteri girisleri ayrismis. |
| Abonelik, modul kilidi, paket yonetimi | Var | `ModulesController`, `SubscriptionRequiredController`, `SlnBaseController` | Paket/modul guard is akisini besliyor. |
| Sube yonetimi ve sube izolasyonu | Var | `BranchesController`, `SlnBranch`, branch selector | Owner global, sube kullanicisi scoped calisiyor. |
| Dashboard aksiyon merkezi | Var | `Home/Index.cshtml`, `SlnDashboard.js`, commit `d2fae66` | KPI disinda bugun yapilacak is uretmeye basladi. |
| Randevu takvimi | Var | `AppointmentsController`, `Appointments.js`, `SlnAppointment` | Bos durum ve waitlist koprusu commit `0da97ad` ile guclendi. |
| Bekleme listesi | Kismi | `WaitlistController`, `Waitlist.js`, `SlnWaitlistEntry` | Manuel kayit ve randevuya cevirme var; otomatik slot onerisi yok. |
| Public salon profili | Var | `PublicSalon/Profile.cshtml`, `PublicProfile.js`, `SlnSalonProfile` | Guven sinyalleri commit `123fb66` ile eklendi. |
| Public online booking | Kismi | `PublicSalon/Book.cshtml`, `PublicBook.js`, public proxy | Hizmet/personel/tarih/slot/waitlist var; server-side funnel raporu yok. |
| Terk edilen booking kurtarma | Kismi | commit `46ce9e0` | Browser-local draft recovery var; salon sahibine rapor/segment yok. |
| Musteri karti ve musteri hafizasi | Var | `ClientsController`, `ClientDetail.js`, `SlnClient` | Wallet ozeti commit `e40d056` ile sadakat/uyelik/paket/hediye karti topluyor. |
| Formul, tedavi kaydi, musteri fotografi | Var | `SlnFormula`, `SlnTreatmentRecord`, `SlnClientPhoto`, `BeforeAfterController` | Gorsel/klinik hafiza parcasi mevcut. |
| KVKK/onam formlari | Var | `ConsentFormsController`, `SlnConsentForm`, `SlnClientConsent` | Salon musteri onam takibi var. |
| Personel, vardiya, izin, yetkinlik, fiyat | Var | `StaffController`, `PersonnelPricesController`, `SlnPersonnelShift`, `SlnPersonnelSkill` | Personel operasyon altyapisi genis. |
| Hizmet, kategori, varyant, combo | Var | `ServicesController`, `Services.js`, `SlnService`, `SlnServiceCombo` | Yogunluk azaltma commit `262413b` ile basladi. |
| Kaynak/oda/cihaz ihtiyaci | Var | `SlnResource`, `SlnServiceResourceRequirement` | Hizmet formunda ileri ayarlar altina alindi. |
| Seans paketi ve kullanim | Var | `PackagesController`, `SlnPackageDefinition`, `SlnPackageUsage` | Musteri wallet icinde gorunur hale geldi. |
| Uyelik planlari | Var | `MembershipsController`, `SlnMembershipPlan`, `SlnClientMembership` | Marketing altinda konsolide edildi. |
| Hizli satis, adisyon, odeme, iade | Var | `SalesController`, `InvoicesController`, `SlnInvoice`, `SlnInvoicePayment`, `SlnInvoiceRefund` | POS/adisyon omurgasi mevcut. |
| Cari hesap / musteri defteri | Var | `SlnClientLedger` | Odeme ve borc takibine baglanabilir. |
| Kasa, kasa acilis/kapanis, gider | Var | `CashController`, `ExpensesController`, `SlnCashRegister`, `SlnExpense` | Rapor aksiyonlarina baglandi. |
| Urun, marka, kategori, sube stogu | Var | `ProductsController`, `SlnProduct`, `SlnProductBranchStock` | Sube bazli stok modeli mevcut. |
| Tedarikci, siparis, hareket | Var | `SuppliersController`, `SlnSupplier`, `SlnSupplierOrder`, `SlnStockMovement` | Finans/stok kararlarini besliyor. |
| Recete ile hizmet stok tuketimi | Var | `RecipesController`, `SlnRecipe`, `SlnRecipeItem` | Hizmet-sarf malzeme baglantisi var. |
| Kampanya ve otomatik hatirlatici | Var | `MarketingController`, `Campaigns.js`, `SlnCampaign`, `SlnAutoReminder` | Sonuc odakli sekme/kart yapisina alindi. |
| E-posta kampanyasi ve ayarlari | Var | `EmailCampaignsController`, `EmailSettingsController`, `SlnEmailCampaign` | Marketing icinde ayni is ailesine baglandi. |
| Winback, sadakat, hediye karti | Var | `WinbackController`, `LoyaltyController`, `GiftCardsController` | Musteri geri kazanma ve cebi parcasi mevcut. |
| Yorum/review akisi | Var | `ReviewsController`, `SlnReview`, `SlnReviewRequest` | Public guven sinyalini besliyor. |
| WhatsApp mesajlasma | Altyapi var | `SlnWhatsAppConfig`, `SlnWhatsAppMessage` | Entity var; owner UX ve gonderim akisi tam urunlesmis gorunmuyor. |
| Reports ve aksiyon onerileri | Kismi | `ReportsController`, `Reports.js`, commit `afefe8e` | Metrikten aksiyona gecis var; otomatik kampanya taslagi yok. |
| No-show/depozito policy | Kismi | `NoShowPolicyController`, `SlnNoShowPolicy` | Genel policy var; hizmet/personel/risk bazli dinamik policy yok. |
| Online odeme / iyzico | Var | `PaymentInfoController`, `iyzico-checkout.js`, module purchase akislari | Booking/abonelik odemelerine bagli. |
| Veri import | Var | `DataImportController`, `DataImport.js` | Eski salon verisini iceri alma akisi var. |
| Public musteri paneli | Var | `PublicSalon/Panel.cshtml`, `PublicPanel.js` | Musteri tarafinda randevu/hesap yuzeyi var. |
| Booking kaynak/kanal raporu | Kismi | Public link/QR altyapisi | `source` parametresi ve Reports dagilimi yok. |
| Queue/self check-in | Yok/Kismi | Waitlist omurgasi | Randevusuz gelen musteri icin canli sira modu yok. |
| AI/business advisor | Yok/Kismi | Kural bazli aksiyon merkezi | AI yok; once kural bazli is onerileri geldi. |

## Urun Gercegi

Salon tarafinda ozellik eksigi sanilandan az; asil sorun ozelliklerin cok parca halinde durmasi ve sahibin "simdi ne yapayim" sorusuna gec cevap vermesiydi. Bu sprintte dashboard, randevu, musteri detay, public booking, public profil, reports, services ve marketing yuzeylerinde bu parcalar daha karar/aksiyon odakli hale getirildi.

## Sonraki En Degerli Bosluklar

| Oncelik | Bosluk | Ilk is |
|---|---|---|
| P1 | Waitlist auto-fill | Bos slot olusunca uygun waitlist kayitlarini Appointments ekraninda oner. |
| P1 | Server-side booking recovery | Public funnel eventlerini API'ye yaz, Marketing icinde yarim kalan booking segmenti ac. |
| P1 | Utilisation campaign draft | Reports aksiyonundan hazir kampanya taslagi ac. |
| P1 | Queue/self check-in | Public profil uzerinden siraya gir; salonda canli queue ekrani. |
| P2 | Booking kaynak raporu | Public linklere `source` parametresi ve Reports dagilimi ekle. |
| P2 | Risk bazli depozito/no-show | Hizmet, personel ve musteri gecmisine gore policy oner. |

## Kapanis

Bu dosya `SALONUXGAP.1` icin feature envanterini kapatir. Runtime kanitlari sabah `docs/salon-ux-qa-protokolu-20260525.md` sirasi ile alinacak.

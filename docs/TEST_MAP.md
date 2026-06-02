# CorpLynk CallCenter — Geniş Test Haritası

Bu doküman CRM, Salon ve Management uygulamalarının uçtan uca regression testi için kullanılır. Her bölüm ayrı bir test runu olarak çalıştırılabilir. Sıra önemli: önce **Setup**, sonra **Smoke**, sonra **Modül bazlı E2E**, en son **Cross-app + Security**.

## 0. Mimari Özet

Üç frontend + tek backend:

| Bileşen | Çalıştırma URL (lokal) | Auth Cookie | Rol Hedefi |
|---|---|---|---|
| API (ASP.NET Core Web API) | `http://localhost:5041` / `https://localhost:7147` | JWT | Tüm uygulamalar buraya bağlanır |
| Salon (MVC) | `http://localhost:5239` | `CorpLynk.Salon.Auth` | Salon sahibi/personel |
| CRM (MVC) | `http://localhost:5176` (varsayılan) | `CorpLynk.Crm.Auth` | CRM kullanıcısı (Genel/Salon/CallCenter scope) |
| Management (MVC) | `http://localhost:5280` | `CorpLynk.Mgmt.Auth` | Platform Admin |

Ana DB: PostgreSQL `CallCenterDB` (lokal: `Host=localhost;Port=5432;Username=postgres`). Migration `Development` veya `AUTO_MIGRATE=true` ortamlarında otomatik. Şu an default açık.

Modül kataloğu:
- `SalonPortalModules` (Salon, 201-228)
- `CrmModules` (CRM, 3 grup: Core 301-310, SalonVertical 401-407, CallCenterVertical 501-506)
- Salon → CRM çeviri: `CrmModules.SalonModuleMap` (örn. SlnGiftCards 216 → SalonGiftCards 401)

Yetki katmanları:
- API: `[Authorize]`, `[RequireModule]`, `[RequireAnyModule]`, `[RequireSalonPage]`
- Salon: `SlnBaseController` + `SalonRolePermissions` + `SalonModuleControllerMap`
- CRM: `CrmBaseController` + `CrmModules` + (ileri) entitlement scope
- Management: `MgmtBaseController` + `[Authorize(Roles="Admin")]`

---

## 1. Setup ve Ortam Hazırlığı

### 1.1. Build + Test
- [ ] `dotnet build CallCenter.slnx` PASS, 0 error
- [ ] `dotnet test tests/CallCenter.Tests/CallCenter.Tests.csproj` PASS (339+ test)
- [ ] `git status --short` temiz veya bilinen değişiklik
- [ ] Migration uygulandı: `AppDbContext` startup'ta `Migrate` çalıştı (log'da görmek için ilk açılışı izleyin)

### 1.2. Test Hesapları
Her senaryo için en az şu hesap setleri:

**Salon tarafı**
- [ ] Salon Sahibi (rol 101) — `salon-owner@test.local`
- [ ] Müdür (102) — `salon-manager@test.local`
- [ ] Şube Müdürü (107) — `salon-bm@test.local` + BranchId claim
- [ ] Kuaför (103) — `salon-hair@test.local`
- [ ] Güzellik Uzmanı (104) — `salon-beauty@test.local`
- [ ] Kasiyer (105) — `salon-cashier@test.local`
- [ ] Resepsiyonist (106) — `salon-recep@test.local`

**CRM tarafı**
- [ ] CRM Owner (CustomerUser) — sadece Core modüller
- [ ] CRM Owner — Core + SalonVertical (Salon CRM paketi)
- [ ] CRM Owner — Core + CallCenterVertical
- [ ] CRM Owner — tüm scope'lar (multi-vertical)

**Management tarafı**
- [ ] Platform Admin (rol 3 — Admin)
- [ ] Personnel Manager (rol 8 — Manager) — yetki ayrımı için

**Müşteri (Customer) bazlı**
- [ ] Yeni kayıt edilmiş, henüz abonelik almamış salon
- [ ] Trial dönemde salon
- [ ] Aktif aboneliği olan salon (tahakkuk açık)
- [ ] Çok şubeli salon (en az 2 branch)
- [ ] Salon + CRM çoklu paket sahibi

### 1.3. Seed Data Doğrulama
- [ ] Salon kaydı sonrası varsayılan veriler: 6 hizmet kategorisi, ~30 örnek hizmet, default branch, ödeme yöntemleri
- [ ] `Defaults` modülleri `CustomerModules` claim'ine yazılmış (isDefault=true 18 Salon modülü)
- [ ] `SalonRolePermissions` 7 rol için sayfa erişim listesi tam

### 1.4. Payment Config
- [ ] Management `/PaymentConfig` → Iyzico Sandbox provider:
  - API Key: `sandbox-Jx1odxGhgv3kpF1bIDFZoyKurI4ifoTt`
  - Secret: `sandbox-RInJWDmjMxA6Nq8jbtzwvWaGzFMrWQYS`
  - Base URL: `https://sandbox-api.iyzipay.com`
- [ ] Banka bilgisi alanları dolu (havale/EFT için)
- [ ] "Bağlantıyı Test Et" → PASS, `LastTestSuccess=true`
- [ ] DB: `PlatformPaymentConfigs.EncryptedCredentials` boş değil, `EncryptedBankInfo` NULL değil

> **Not**: Yeni PC/temiz DB'de CreateAsync API key boş kabul ediyor (bug izi #BUG2-CREATE). Kayıt sonrası Decrypt edip `ApiKey != ""` doğrulayın.

---

## 2. Smoke Test Turu (15 dakika)

Her uygulama için minimum hayatta kalma:

### 2.1. API
- [ ] `GET /api/version` veya `GET /healthz` 200
- [ ] `POST /api/auth/login` (geçerli kullanıcı) → JWT
- [ ] `POST /api/auth/login` (yanlış şifre) → 401, brute force counter artıyor
- [ ] OpenAPI Development modunda açık (`/swagger`)
- [ ] SignalR `/hubs/callcenter` connect

### 2.2. Salon
- [ ] `/Account/Login` → JWT cookie → `/Home` dashboard
- [ ] Sidebar grupları doğru render (Salon, Operasyon, Stok, Finans, Müşteri İlişkileri, Raporlar, Yönetim)
- [ ] Logout → cookie temizlenir

### 2.3. CRM
- [ ] `/Account/Login` → cookie → `/Home` (scope'a göre Salon, Genel veya CallCenter dashboard)
- [ ] Sidebar entitlement'lara göre filtrelenmiş
- [ ] Logout

### 2.4. Management
- [ ] `/Account/Login` → Admin değilse 403
- [ ] Admin login → `/Home`
- [ ] Tüm yönetim sayfaları sidebar'da

---

## 3. Salon — Modül Bazlı E2E (P0)

Her sayfa için 3 senaryo: **Görüntüleme**, **CRUD**, **Yetki/Modül engeli**.

### 3.1. Hızlı Satış POS (`/Sales` — modül 214)
- [ ] Sayfa açılır, kategori chip'leri yüklenir
- [ ] Hizmet chip tıklama → sepete ekle (KO foreach düzgün render)
- [ ] Ürün ekleme + reçete otomatik malzeme tüketimi
- [ ] Müşteri seçimi → autocomplete çalışır (`createAutocomplete` pattern)
- [ ] Sadakat Puanı paneli (varsa): puan girince TL hesabı doğru
- [ ] Sadakat Paketi (A) chip → cart'a 0 TL kalemi ekler, satış sonrası purchase oluşur
- [ ] Çok Seanslı Hizmet (B) plan: seans tüketimi `EarnFromInvoiceItemsAsync`/`RecordSessionAsync` çağrılır
- [ ] Sadakat Programı (D) reward chip → cart'ta 0 TL, ApplyRewardAsync invoiceItem'a bağlanır
- [ ] **Tahsilat Al** → ödeme yöntemi seçimi (nakit/kart/havale/hediye kartı/üyelik)
- [ ] Adisyon oluştu (`Invoice id, no, netAmount`)
- [ ] Kasa hareketi yazıldı (`SlnCashTransaction`)
- [ ] C-Earn: pointsPerTL × NetAmount kadar puan eklendi (snowball yok)
- [ ] **Kuralı doğrula**: 150 TL, 200 puan kullan, 130 TL nakit → bakiye 430

### 3.2. Müşteriler (`/Clients` — modül 202)
- [ ] Liste, pagination, arama, sıralama
- [ ] Müşteri ekle (zorunlu alan validation)
- [ ] Detay sayfası: bilgileri, geçmiş randevular, adisyonlar, sadakat bakiyesi
- [ ] Engelli müşteri badge'i (no-show count, blacklisted)
- [ ] Şube izolasyonu: Şube Müdürü sadece kendi şubesindeki müşterileri görür

### 3.3. Randevular (`/Appointments` — modül 203)
- [ ] Takvim görünümü (gün/hafta/ay)
- [ ] Boş slot bulma: `GetAvailableSlotsAsync` personel-öncelikli, branch fallback
- [ ] Randevu oluşturma (müşteri + personel + hizmet + tarih)
- [ ] Çakışma kontrolü (aynı personel, kesişen saat)
- [ ] Personel çalışma saatleri override edilirse slot filtresi
- [ ] Engelli müşteri kırmızı badge, no-show count sarı badge
- [ ] Status değişimi: Onaylandı → Geldi → Tamamlandı / İptal / No-Show
- [ ] No-Show → ceza/depozito uygulanır mı (NoShowPolicy varsa)

### 3.4. Bekleme Listesi (`/Waitlist` — modül 221)
- [ ] Liste, ekleme
- [ ] Slot boşaldığında otomatik bildirim akışı (varsa)

### 3.5. Hizmetler / Reçeteler / Personel Fiyatları
- [ ] `/Services` (204): kategori CRUD, hizmet CRUD, multi-session sessionCount
- [ ] `/Recipes` (215): reçete tanımı, malzeme + miktar
- [ ] `/PersonnelPrices` (228): personel × hizmet override fiyatlar
- [ ] `/Staff` (209): personel CRUD, çalışma saatleri (workingDays + override)

### 3.6. Stok (`/Products` 205, `/Suppliers` 210)
- [ ] Ürün CRUD, stok seviyesi
- [ ] Tedarikçi CRUD, cari bakiye
- [ ] Düşük stok alert (varsa)
- [ ] Reçete tüketimi sonrası stok eksilir (Sales akışından)

### 3.7. Finans
- [ ] `/Invoices` (206): adisyon listesi, detay, iptal
- [ ] `/Cash` (207): kasa hareketleri, gün sonu kapama, açık kasa kontrolü
- [ ] `/Expenses` (208): masraf CRUD, kategori
- [ ] `/GiftCards` (216): kart oluşturma, bakiye sorgulama, satın alma akışı, harcama
- [ ] **Sadakat Paketleri (`/LoyaltyPackages` 217 — Operasyon grubunda)**:
  - Paket tanımla (10 öde 12 al gibi)
  - Müşteri satın alma → kredi bakiyesi (`SlnLoyaltyPackagePurchase`)
  - Adisyondan kullanım → kredi düşer
  - Bakiye sıfırlanınca expire / pasif

### 3.8. Müşteri İlişkileri (Marketing Composite — `/Marketing`)
- [ ] Kampanyalar (SMS, 212)
- [ ] Üyelik Planları (218) — satış akışı, indirim/seans paketi
- [ ] Hediye Kartları (216)
- [ ] E-posta Kampanyaları (222)
- [ ] Yorumlar (223)
- [ ] Geri Kazanım (227)
- [ ] **Sadakat Programı tab YOK** (CRM'e taşındı)
- [ ] **Sadakat sekmesi yok** (CRM'e taşındı)

### 3.9. Yönetim (Salon Owner only)
- [ ] `/Profile` (220): salon profili, slug, çalışma saatleri
- [ ] `/Branches` (213): şube CRUD, branch slug, default branch
- [ ] `/NoShowPolicy` (224): politika tanımı, ceza/depozito kuralı
- [ ] `/ConsentForms` (225): form CRUD, müşteri imzası
- [ ] `/BeforeAfter` (226): fotoğraf yükleme, müşteri × hizmet
- [ ] `/EmailSettings`: SMTP/OAuth (Gmail/Outlook) ayarları
- [ ] `/PageSettings`: public salon sayfası ayarları
- [ ] `/PaymentInfo`: iyzico sub-merchant onboarding (PS.5)
- [ ] `/DataImport`: eski salon verisi importu

### 3.10. Raporlar (`/Reports` — 211)
- [ ] Günlük/aylık ciro
- [ ] Personel hak ediş (settlement)
- [ ] Hizmet popülerlik
- [ ] Müşteri retansiyon

### 3.11. Public Salon (Anonim akış)
- [ ] `/s/{slug}` profile sayfası
- [ ] Randevu alma: hizmet/personel/tarih seçimi, deposit (varsa)
- [ ] Üyelik satın alma (Iyzico checkout)
- [ ] ServiceCombos / kombo paketler — mobil görünüm
- [ ] Public proxy CSRF/XSS koruma

### 3.12. Salon Rol Bazlı Erişim (matris)

| Sayfa | 101 Sahip | 102 Müdür | 103 Kuaför | 104 Uzman | 105 Kasiyer | 106 Resepsiyon | 107 Şube Müdürü |
|---|---|---|---|---|---|---|---|
| Home | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Sales | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Clients | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Appointments | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Waitlist | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Services | ✓ | ✓ | – | – | – | – | – |
| Staff | ✓ | ✓ | – | – | – | – | ✓ |
| Recipes | ✓ | ✓ | ✓ | ✓ | – | – | – |
| PersonnelPrices | ✓ | ✓ | – | – | – | – | – |
| LoyaltyPackages | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ |
| Products | ✓ | ✓ | – | – | – | – | – |
| Suppliers | ✓ | ✓ | – | – | – | – | – |
| Invoices | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ |
| Cash | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ |
| Expenses | ✓ | ✓ | – | – | – | – | ✓ |
| GiftCards | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ |
| Marketing | ✓ | ✓ | – | – | – | – | – |
| Reports | ✓ | ✓ | – | – | – | – | ✓ |
| Profile/Branches/Modules/PaymentInfo | ✓ | – | – | – | – | – | – |

- [ ] Her hücre için: doğru rolle giriş → sayfa açılır; yanlış rolle giriş → 403 veya ModuleRequired
- [ ] Sidebar'da görünmeyen sayfaya URL ile gidiş engellenmeli
- [ ] Şube Müdürü `/Branches` URL'ine gitse 403, `/Clients` URL'ine gitse sadece kendi şubesini görür (BranchId claim filtresi)

### 3.13. Modül Bazlı Engel
- [ ] Modülü olmayan müşteri o sayfaya gidince `/ModuleRequired?moduleId=X` redirect
- [ ] Müdür panelinden modül satın alma → tekrar dener → sayfa açılır
- [ ] JWT refresh sonrası yeni modüller claim'e yazılmış olmalı

---

## 4. CRM — Scope Bazlı E2E (P0)

### 4.1. CRM Genel / Core (modüller 301-310)
Sadece Core scope satın alınmış kullanıcıyla:
- [ ] `/Home` — Genel Dashboard
- [ ] `/Contacts` (302): kişi CRUD, etiket, segment, import/export
- [ ] `/Tickets` (303): talep CRUD, status, atama, SLA
- [ ] `/Deals` (304): fırsat kanban, stage geçişleri
- [ ] `/Activities` (305): etkileşim timeline (çağrı, e-posta, not)
- [ ] `/Tasks` (306) / `/CrmTasks`: görev CRUD, atama, deadline
- [ ] `/Surveys` (307): anket CRUD, dağıtım
- [ ] `/Campaigns` (308): kampanya CRUD, hedef segment, gönderim
- [ ] `/Reports` (309): rapor matrisi
- [ ] `/Integrations` (310): bağlı sistemler, OAuth flow
- [ ] **Sidebar'da Salon ve CallCenter grupları görünmemeli**

### 4.2. CRM SalonCrm Scope (modüller 401-407 + 403=Sadakat)
Salon CRM paketi satın alınmış kullanıcıyla:
- [ ] `/Home/Salon` veya `/Home` salon vertical dashboard'a düşer
- [ ] Sidebar "Salon" grubu açık, link'ler:
  - Sadakat → `/SalonCrm/Loyalty`
  - Üyelikler → `/SalonCrm/Memberships`
  - Hediye Kartları → `/SalonCrm/GiftCards`
  - Kampanyalar → `/SalonCrm/Campaigns`
  - E-posta Kampanyaları → `/SalonCrm/EmailCampaigns`
  - Yorumlar → `/SalonCrm/Reviews`
  - Kayıp Müşteri → `/SalonCrm/Winback`

**SalonCrm/Loyalty (2 tab)**
- [ ] Sadakat Puanı tab (C):
  - Config (pointsPerTL, pointValue, minRedeemPoints, isActive) kaydedilir
  - Müşteri puan listesi: earned/spent/balance/TL karşılığı
  - Salon /Sales'tan kazanılan puanlar burada görünür
  - Salon /Sales'tan harcanan puanlar burada düşer
- [ ] Sadakat Programı tab (D):
  - **+ Yeni Program** butonu modal açar (Ad, Sayılan Hizmet, Eşik, Ödül Hizmeti, Aktif)
  - Program listesi, Düzenle/Sil butonları çalışır
  - Müşteri ilerleme tablosu: visit count, rewards earned/available
  - **Expired reward "Kalan" sayımına dahil DEĞİL** (commit `c3c3d35`)

**SalonCrm/Memberships, GiftCards, Reviews, Winback, Campaigns**
- [ ] Liste + detay + CRUD (her birinde)
- [ ] Salon backend ile aynı entity'lerden besleniyor (CrmSalonController adapter)
- [ ] Şube izolasyonu: BranchId claim > query param

### 4.3. CRM CallCenterCrm Scope (modüller 501-506)
CallCenter CRM paketi satın alınmış kullanıcıyla:
- [ ] `/Home/CallCenter` dashboard
- [ ] Çağrı kişileri, destek talepleri, etkileşimler, arama kampanyaları, raporlar
- [ ] Çağrı entegrasyonu: SignalR `/hubs/callcenter` üzerinden gelen aktif çağrı → CRM ekranında pop-up/notification

### 4.4. CRM Multi-Scope
Hem Core + SalonVertical + CallCenterVertical satın alan kullanıcı:
- [ ] Sidebar 3 grup birden açılır
- [ ] Dashboard hangi scope'a düşer? (varsayılan + manuel geçiş)
- [ ] Cross-app navigation switcher (sol üst) — Salon ↔ CRM ↔ CallCenter geçişi

### 4.5. CRM Auth ve Hesap
- [ ] Login (CRM domain)
- [ ] Şifre sıfırlama (`/Account/ForgotPassword`)
- [ ] Email doğrulama (`/Account/VerifyEmail`) — AUTH-6 commit
- [ ] Logout
- [ ] Concurrent session (aynı kullanıcı 2 tarayıcı)

### 4.6. CRM /Payments (Unified Billing Checkout)
- [ ] `/Payments` açık tahakkukları listeler
- [ ] "Öde" modal açılır, kalemler ve toplam görünür
- [ ] "Onaylıyorum, ödemeye geç" → Iyzico hosted page açılır (`paymentPageUrl` veya iframe)
- [ ] Test kartla başarılı ödeme → callback → result modal → tahakkuklar kapanır
- [ ] Test kartla başarısız ödeme → result modal → tahakkuklar açık kalır
- [ ] **CSP**: `script-src 'self' ... https://*.iyzipay.com https://*.iyzico.com` (commit `c92dbaa`)
- [ ] Tarayıcı Console **CSP violation YOK**

---

## 5. Management — Modül Bazlı E2E (P0)

### 5.1. Müşteri Yönetimi
- [ ] `/Customers` liste, filtre, arama, pagination
- [ ] `/Customers/Detail/{id}`: müşteri özet, modülleri, kullanıcıları, ödemeleri, tahakkukları
- [ ] **Toplam Aylık** hesabı doğru (PRICING.9 fix sonrası grup/paket fiyatlandırma)
- [ ] Müşteri ekleme/düzenleme/pasif
- [ ] Modül atama/çıkarma
- [ ] Manual abonelik aktivasyon

### 5.2. Modül ve Paket Yönetimi
- [ ] `/Modules` liste (CallCenter portal modülleri)
- [ ] `/ServiceManagement` veya benzer: Salon ve CRM modülleri
- [ ] Modül fiyatları (`PricingPeriods`): geçmiş ve gelecek dönem fiyatları
- [ ] Modül grupları/paketler tanımı
- [ ] Müşteri talepleri (`SlnModuleRequest`)
- [ ] Modül envanteri (satın alınma sayısı, gelir)

### 5.3. Kullanıcı/Personel/Organizasyon
- [ ] `/Users` platform kullanıcıları
- [ ] `/Personnel` çalışanlar
- [ ] `/Organizations` firma yapısı

### 5.4. Ödeme Yapılandırma (`/PaymentConfig`)
- [ ] Provider listesi (Iyzico/PayTR/Param)
- [ ] **CreateAsync**: bilgileri girip kaydet → DB'de EncryptedCredentials dolu olmalı (CreateAsync boş key geçirmemeli; bilinen bug: validation eksik — issue açın)
- [ ] **UpdateAsync**: edit'te boş bırakırsan eski korunur (BUG2.18 fix)
- [ ] **TestConnectionAsync**: aktif config'le, gerçek Iyzico API çağrısı
- [ ] Banka bilgisi (BankName, IBAN, AccountHolder, Description)
- [ ] Aktif/pasif toggle

### 5.5. Sub-Merchant (PS.4–PS.7)
- [ ] `/SubMerchants` salon onboarding kayıtları
- [ ] Pazaryeri split testi: ödeme alımında basketItem.subMerchantKey ve subMerchantPrice eklenmesi
- [ ] Hak ediş raporu (`/BillingReport` — PS.10 + PS.13)

### 5.6. Email Template + Storage Config + Translations
- [ ] `/EmailTemplates` CRUD, preview, gönderme
- [ ] `/StorageConfig` cloud storage ayarları
- [ ] `/Translations`: i18n key/value editör, **Reload Cache** → API + Salon server-side cache yenilenir (SALONI18N.9)

### 5.7. KVKK + Audit
- [ ] `/Kvkk` veri imha talepleri
- [ ] `/AuditLogs` filtre, arama, export
- [ ] Sensitive alanların maskelendiği doğrula

### 5.8. Lisanslama + Bildirimler
- [ ] `/Licensing` müşteri lisans durumları
- [ ] `/Notifications` platform bildirimleri

---

## 6. Cross-App E2E Senaryoları (P0)

### 6.1. Yeni Salon Onboarding
1. [ ] Landing `/register/salon` → form doldur → kayıt
2. [ ] Trial otomatik açıldı, default modüller atandı, default veri seedi
3. [ ] Salon `/Account/Login` → JWT → `/Home` dashboard
4. [ ] Onboarding video/wizard akışı (SALONONBOARD.3)
5. [ ] İlk randevu, ilk adisyon, ilk kasa kapama

### 6.2. Yeni Salon + Salon CRM Paketi
- [ ] Salon hesabı var, CRM paketi yok → CRM'e login olunca SalonCrm linkleri görünmez
- [ ] Management'tan SalonCrm paketi atanır
- [ ] CRM yeniden login → SalonCrm linkleri görünür
- [ ] `/SalonCrm/Loyalty` → 2 tab, Sadakat Programı oluşturma çalışır

### 6.3. Tahakkuk → Ödeme Akışı
1. [ ] Customer aylık tahakkuk açıldı (1700 TL salon platform paketi)
2. [ ] Salon kullanıcısı CRM `/Payments` → Öde
3. [ ] Iyzico sandbox checkout → test kart → 3DS → callback
4. [ ] Tahakkuk **Ödendi** statüsüne geçti
5. [ ] Customer Detail'da "Toplam Aylık" doğru
6. [ ] PaymentTransactions tablosunda kayıt
7. [ ] Iyzico webhook simülasyonu (settlement event) → audit trail

### 6.4. Sadakat Akışı (4 kavram entegrasyonu)
- [ ] Müşteri yeni eklenir
- [ ] Salon /Sales: 500 TL hizmet satılır → C (Sadakat Puanı) 500 puan oluşur
- [ ] Salon /LoyaltyPackages: 10 öde 12 al paket satışı (A) → kredi bakiyesi
- [ ] Salon /Sales: B (multi-session hizmet) "8 seans lazer" satışı → plan oluşur
- [ ] CRM /SalonCrm/Loyalty: program oluştur (D — "10 fön → 1 bedava")
- [ ] Salon /Sales: 10 fön satıldı → reward oluştu
- [ ] 11. fön satışı: reward chip cart'ta → 0 TL kalem
- [ ] CRM /SalonCrm/Loyalty: progress tablosunda doğru sayılar

### 6.5. Çok Şubeli Salon
- [ ] Salon Owner 2 şube tanımlar
- [ ] Şube Müdürü A şubesi için tanımlanır (BranchId=2)
- [ ] Şube Müdürü login → sadece A şubesi randevu/müşteri/cash görür
- [ ] Sahip → tüm şubeler veya seçici ile geçiş
- [ ] CRM Salon vertical owner → branch selector (CRMPROD.9)

### 6.6. Çağrı Akışı (CallCenter)
- [ ] Inbound call → SignalR notification CRM'e gelir
- [ ] Agent çağrıyı cevaplar → ticket otomatik oluşur
- [ ] Çağrı kaydı (recording) Cloud Storage'a yüklenir
- [ ] Çağrı sonrası supervisor görüntüleyebilir

### 6.7. Public Salon → Customer
- [ ] `/s/{slug}` profile sayfasına anonim ziyaret
- [ ] Hizmet kombosu seçer (MOBDATA.1)
- [ ] Tarih/slot seçer (gerçek `GetAvailableSlotsAsync`)
- [ ] Telefon doğrulama (OTP varsa)
- [ ] Randevu oluşur, Salon tarafından görünür
- [ ] No-show deposit varsa Iyzico üzerinden alınır

---

## 7. Negative / Edge Cases (P1)

### 7.1. Auth ve Güvenlik
- [ ] JWT expired → 401 → login redirect
- [ ] Cookie tampered → 401, logout
- [ ] Cross-tenant: A müşterinin verilerini B müşteri JWT'siyle çağırma → 403/404
- [ ] SQL injection denemeleri form alanlarında
- [ ] XSS deneme: arama kutusu, müşteri adı, mesaj alanı
- [ ] CSRF: form post'ları, anti-forgery token
- [ ] Brute force: 10 yanlış şifre → kilit
- [ ] Proxy SSRF koruma: `/proxy/...` external URL'ye atış engellenir (4fec31f)

### 7.2. Data Validation
- [ ] Boş/null alan: zorunlu alan boşsa 400
- [ ] Çok uzun string (>255 char)
- [ ] Geçersiz email/telefon format
- [ ] Negative number where positive expected
- [ ] Date past/future limits
- [ ] Concurrent edit (aynı entity 2 user)

### 7.3. UI Davranışı
- [ ] Native `alert/confirm/prompt` yok — sadece `confirmModal` + `toastr`
- [ ] AJAX 401 → global handler login redirect
- [ ] AJAX 403 → sayfanın kendi `.fail` handler'i
- [ ] KO foreach: `$data.prop` ile güvenli erişim
- [ ] `createAutocomplete` `xxxAutocomplete.query` pattern (with binding değil)
- [ ] Razor `@page` directive ile çakışan değişken adı yok

### 7.4. Translations
- [ ] Yeni eklenen TR key'in EN karşılığı var
- [ ] Eksik key → fallback (anahtar adı veya defaultText)
- [ ] Management translation reload → Salon server-side cache yenilenir

### 7.5. Tarih ve TZ
- [ ] UTC çift dönüşüm yok (BUG2.17 fix)
- [ ] Türkiye UTC+3 sabit, DST yok
- [ ] JS → API tarih: UTC suffix tutarlı

---

## 8. Security / CSP / Headers (P0)

### 8.1. Response Headers (her uygulamada)
- [ ] `X-Content-Type-Options: nosniff`
- [ ] `X-Frame-Options: SAMEORIGIN`
- [ ] `Referrer-Policy: strict-origin-when-cross-origin`
- [ ] `Permissions-Policy: camera=(), microphone=(), geolocation=(self)`
- [ ] `Content-Security-Policy:` script-src include iyzipay/iyzico (commit `c92dbaa`)
- [ ] Strict-Transport-Security (production)

### 8.2. CSRF
- [ ] MVC form POST'larında anti-forgery token
- [ ] AJAX POST: header'a token ekleniyor mu

### 8.3. Webhook İmza
- [ ] Iyzico webhook signature verify (`IyzicoWebhookSignatureValidator`)
- [ ] Geçersiz imza → 401

### 8.4. API Key (Integration)
- [ ] `/api/integration/v1/*` `X-Api-Key` middleware kontrolü
- [ ] Eksik/geçersiz key → 401

### 8.5. Encryption at rest
- [ ] `PlatformPaymentConfig.EncryptedCredentials` AES-256-CBC
- [ ] SIP password encrypted
- [ ] Cloud storage tokens encrypted

---

## 9. Performance / Smoke (P2)

- [ ] CRM Contacts 10000+ kayıtla liste pagination süresi (< 2s)
- [ ] Salon Randevu Takvimi aylık görünüm 500+ randevu (< 3s)
- [ ] Reports/Settlement raporu büyük veri (< 5s)
- [ ] SignalR ile 50+ eşzamanlı agent
- [ ] Public Salon profil sayfası TTFB (< 1s)
- [ ] Mobil responsive: 320px–768px (UX.4)

---

## 10. Deploy Kabul Kapısı

### 10.1. Pre-deploy Checklist
- [ ] Root Dockerfile API'ye ait (`head -3 Dockerfile` doğrula)
- [ ] `git status --short` temiz
- [ ] Build + 339+ test PASS
- [ ] Browser smoke (bu doc Bölüm 2)
- [ ] Migration eski/yeni uyumlu (varsa)
- [ ] ClaudeManager Notes'tan ilgili deploy notu okundu

### 10.2. Deploy (gcloud)
- [ ] API: `--update-env-vars` (asla `--set-env-vars`)
- [ ] Salon: Dockerfile geçici değiştirildi → deploy → **hemen API Dockerfile'a geri alındı**
- [ ] Management aynı
- [ ] CRM aynı

### 10.3. Post-deploy Smoke
- [ ] cc-api.corplynk.com `/healthz` 200
- [ ] sln.corplynk.com login + dashboard
- [ ] mng.corplynk.com Admin login
- [ ] CRM endpoint login + Contacts
- [ ] Iyzico canlı test ödeme (1 TL)
- [ ] Webhook gerçek event geliyor

---

## 11. Regression Matrisi (Hızlı Tur — 30 dakika)

Şu PR'larda değişen kritik akışlar:

| Commit | Akış | Test |
|---|---|---|
| `c92dbaa` | CRM /Payments Iyzico checkout | 4.6 |
| `c3c3d35` | CRM SalonCrm progress AvailableRewards (expired hariç) | 4.2 |
| `2708274` | CRM SalonCrm Loyalty Program UI Create/Edit/Delete | 4.2 |
| `70a4d5d` | Salon /Sales C earn invoice akışı | 3.1 |
| `6d73a2c` | Salon /Marketing loyalty tab kaldırıldı | 3.8 |
| `dd96483` | Salon sidebar LoyaltyPackages Operasyon grubu + role gate | 3.7 |
| `7b62697` | Salon Loyalty UI silindi (CRM'e taşındı) | 3.8 |
| `019b907` | Translation cache revalidation | 5.6 |
| `1b515cf` | CRM+Management VerifyEmail/ForgotPassword | 4.5 |
| `cfe6b5a` | Appointment badge (blacklisted, no-show) | 3.3 |

---

## 12. Bilinen Riskler / Açık Konular

- **CreateAsync (PaymentConfig)**: boş API key/secret kabul ediyor (UI mapping veya backend validation). UpdateAsync güvenli.
- **CRM Salon vertical sube izolasyonu**: Owner için branch selector UI yok, ilk MVP allBranches davranışı.
- **CRM ürün lisanslama (CRMPROD.7)**: Paket aktivasyon/fiyatlama semantiği netlik bekliyor.
- **Browser E2E otomasyonu**: in-app browser localhost ERR_BLOCKED_BY_CLIENT — Codex tarafında manuel.
- **Default paket eğitim videosu (SALONONBOARD.3)**: ekran kayıt/sayfa akışı eksik.
- **Mobil responsive testi (UX.4)**: 320-768px görsel kontrol yapılmadı.
- **cekmekoy-sube serviceCombos boş (MOBDATA.1)**: P0 production datası.

---

## 13. Test Çalışma Sırası

1. **Bölüm 1** Setup (her test için bir kere) — 30 dk
2. **Bölüm 2** Smoke (her oturum başında) — 15 dk
3. **Bölüm 3.x** Salon modülleri — 4 saat
4. **Bölüm 4.x** CRM scope'ları — 3 saat
5. **Bölüm 5.x** Management — 2 saat
6. **Bölüm 6** Cross-app E2E — 3 saat
7. **Bölüm 7** Negative/edge — 2 saat
8. **Bölüm 8** Security — 1 saat
9. **Bölüm 9** Performance smoke — 1 saat
10. **Bölüm 10** Deploy kabul — release öncesi
11. **Bölüm 11** Regression hızlı tur (her PR sonrası) — 30 dk

Toplam tam kapsam: **~16 saat** (2 mesai günü).

---

## 14. Sonuç Raporu Şablonu

Her test bölümü sonunda:

```
[Bölüm X.Y Başlık]
PASS / FAIL / BLOCKED
- Adım 1: ✓/✗
- Adım 2: ✓/✗
Notlar:
Ekran görüntüsü: <path>
Konsol/log:
```

Her FAIL/BLOCKED için ClaudeManager'a:
- Pattern (öğrenilen hata)
- Task (düzeltilecek bug)
- Journal (gözlem)

---

**Doküman versiyonu:** 1.0 — 2026-06-02
**Son güncelleme:** Iyzico checkout fix turunda hazırlandı
**Onay:** PR sonrası bu doc da güncellenmeli (yeni sayfa eklendi vb.)

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
- [x] `dotnet build CallCenter.slnx` PASS, 0 error
- [x] `dotnet test tests/CallCenter.Tests/CallCenter.Tests.csproj` PASS (339+ test)
- [x] `git status --short` temiz veya bilinen değişiklik
- [ ] Migration uygulandı: `AppDbContext` startup'ta `Migrate` çalıştı (log'da görmek için ilk açılışı izleyin)

Codex 2026-06-02: Build PASS (0 error, 4 existing Windows warnings). Test PASS: 339/339. `dotnet test` sandbox network kisitina takildi; dis ag izniyle ayni resmi komut gecti.

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
- [x] `GET /api/version` veya `GET /healthz` 200
- [x] `POST /api/auth/login` (geçerli kullanıcı) → JWT
- [x] `POST /api/auth/login` (yanlış şifre) → 401, brute force counter artıyor
- [ ] OpenAPI Development modunda açık (`/swagger`)
- [ ] SignalR `/hubs/callcenter` connect

Codex 2026-06-02: API smoke kismi PASS: mevcut kodda health endpoint'i `/health`, 200 dondu; haritadaki `/healthz` ve `/api/version` 404. Valid login JWT dondu, yanlis sifre 401 dondu. SignalR token ile negotiate 200 dondu, full websocket connect ayrica kosulmadi. OpenAPI FAIL: `/swagger` 404, `/openapi/v1.json` 400 schema depth hatasi donuyor.

Codex 2026-06-03 devam: API smoke endpoint FIX/PENDING API RESTART. Mevcut `/health` korunarak `/healthz` alias'ı ve auth gerektirmeyen `/api/version` endpoint'i eklendi. `/api/version` `name`, assembly `version`, `environment`, `timestamp` döner. Doğrulama: `dotnet build src\CallCenter.Api\CallCenter.Api.csproj -o .codex-build\api-health-version-aliases -p:UseSharedCompilation=false -m:1` PASS. API restart sonrası `/healthz` + `/api/version` canlı 200 retest yapılacak.

Codex 2026-06-03 retest: API smoke endpoint PASS. Restart sonrası `GET http://localhost:5041/healthz` 200 döndü (`{"status":"healthy",...}`), `GET http://localhost:5041/api/version` 200 döndü (`name=CallCenter.Api`, `version=1.0.0.0`, `environment=Development`). 

### 2.2. Salon
- [x] `/Account/Login` → JWT cookie → `/Home` dashboard
- [ ] Sidebar grupları doğru render (Salon, Operasyon, Stok, Finans, Müşteri İlişkileri, Raporlar, Yönetim)
- [x] Logout → cookie temizlenir

Codex 2026-06-02: Salon login/logout PASS. `/Account/Login` mevcut cookie ile `/Home` dashboard'a gecti; logout `/Account/Login?loggedOut=1` sayfasina dondu; tekrar login basarili. Sidebar snapshot'ta Salon, Operasyon, Stok, Finans, Yonetim var. Haritadaki Musteri Iliskileri/Raporlar gruplari mevcut UI'da yok; onceki CRM'e tasima karariyla uyumlu olabilir, bu satir bu nedenle acik birakildi. Screenshot: `.codex-run/screenshots/2-2-salon-dashboard.png`, `.codex-run/screenshots/2-2-salon-logout.png`.

### 2.3. CRM
- [x] `/Account/Login` → cookie → `/Home` (scope'a göre Salon, Genel veya CallCenter dashboard)
- [x] Sidebar entitlement'lara göre filtrelenmiş
- [x] Logout

Codex 2026-06-02: CRM smoke PASS. `codexkokobuyer` ile login `/Home/Salon` dashboard'a gitti. Sidebar Salon CRM entitlement'lariyla filtreli render oldu: Sadakat, Uyelikler, Hediye Kartlari, Pazarlama ve SMS, E-posta Kampanyalari, Yorum Yonetimi, Kayip Musteri. Logout `/Account/Login` sayfasina dondu. UI copy notu: login linkinde ekranda `Şifremi unuttüm` gorunuyor; dogrusu `Şifremi unuttum` olmali. Screenshot: `.codex-run/screenshots/2-3-crm-login.png`, `.codex-run/screenshots/2-3-crm-dashboard.png`, `.codex-run/screenshots/2-3-crm-logout.png`.

### 2.4. Management
- [ ] `/Account/Login` → Admin değilse 403
- [x] Admin login → `/Home`
- [x] Tüm yönetim sayfaları sidebar'da

Codex 2026-06-02: Management admin smoke PASS. Kullanici admin olarak giris yapti; dashboard `http://localhost:5280/` uzerinde acildi (`/Home` yerine root dashboard). Sidebar'da Musteri Yonetimi, Odeme Takibi, Sistem, Finans, KVKK, Gelismis gruplari ve ana yonetim linkleri gorundu. Non-admin denemesinde 403 status yerine login ekraninda `Bu panele sadece Admin rolu ile giris yapilabilir.` mesaji gorundu; bu nedenle ilk satir exact 403 beklentisi olarak acik birakildi. Screenshot: `.codex-run/screenshots/2-4-mgmt-login.png`, `.codex-run/screenshots/2-4-mgmt-nonadmin.png`, `.codex-run/screenshots/2-4-mgmt-admin-dashboard.png`.

---

## 3. Salon — Modül Bazlı E2E (P0)

Her sayfa için 3 senaryo: **Görüntüleme**, **CRUD**, **Yetki/Modül engeli**.

### 3.1. Hızlı Satış POS (`/Sales` — modül 214)
- [x] Sayfa açılır, kategori chip'leri yüklenir
- [x] Hizmet chip tıklama → sepete ekle (KO foreach düzgün render)
- [x] Ürün ekleme + reçete otomatik malzeme tüketimi
- [x] Müşteri seçimi → autocomplete çalışır (`createAutocomplete` pattern)
- [x] Sadakat Puanı paneli (varsa): puan girince TL hesabı doğru
- [x] Sadakat Paketi (A) chip → cart'a 0 TL kalemi ekler, satış sonrası purchase oluşur
- [x] Çok Seanslı Hizmet (B) plan: seans tüketimi `EarnFromInvoiceItemsAsync`/`RecordSessionAsync` çağrılır
- [x] Sadakat Programı (D) reward chip → cart'ta 0 TL, ApplyRewardAsync invoiceItem'a bağlanır
- [x] **Tahsilat Al** → ödeme yöntemi seçimi (nakit/kart/havale/hediye kartı/üyelik)
- [x] Adisyon oluştu (`Invoice id, no, netAmount`)
- [x] Kasa hareketi yazıldı (`SlnCashTransaction`)
- [x] C-Earn: pointsPerTL × NetAmount kadar puan eklendi (snowball yok)
- [x] **Kuralı doğrula**: 150 TL, 200 puan kullan, 130 TL nakit → bakiye 430

Codex 2026-06-02: `/Sales` POS temel akisi PASS. Kategori chip'leri ve Seans Satislari yuklendi. `Sac Kesim` sepete eklendi; checkout ilk denemede sarf bilgisi zorunlulugunu dogru yakaladi ve modal acti. `Malzeme yok` secilince kalem `Sarf yok` oldu. Musteri autocomplete klavye secimiyle calisti; `Codex CRM Compat 20260602223717` secilince uyelik avantaji otomatik 0 TL uyguladi, bu nedenle normal nakit testi icin musteri temizlenip kalem yeniden eklendi. Hızlı müşteri `Codex POS Smoke 20260602` ile Nakit 150 TL tahsilat tamamlandi. `/Invoices`: `SLN-20260602-0002`, `Sac Kesim`, `150 TL`, `Nakit`, `Odendi`. `/Cash`: Ana Kasa hareketlerinde `Adisyon: SLN-20260602-0002`, `150 TL` gelir satiri gorundu. UI copy notu: sarf uyarisi toast'inda `kullanildigini/secin` karakterleri Turkce degil. Screenshot: `.codex-run/screenshots/3-1-sales-initial.png`, `3-1-sales-after-service.png`, `3-1-sales-sarf-modal.png`, `3-1-sales-material-required.png`, `3-1-sales-completed.png`, `3-1-invoices-after-sale.png`, `3-1-cash-ana-transactions.png`.

Codex 2026-06-03 C loyalty retest: PASS. Onkosul olarak config aktif hale getirildi (`pointsPerTL=1`, `pointValue=0.1`, `minRedeemPoints=100`, `isActive=true`). `/Sales` UI'da `Codex POS Smoke 20260602` musterisine `Hydrafacial` 500 TL Nakit satildi; sarf guard `Malzeme yok` ile gecildi, toast `Ödeme alındı`, console error yok. API dogrulama: `currentBalance=500`, transaction `Earn 500`, `balanceValue=50.0`. Sonra ayni musteride `Saç Kesim` 150 TL sepete eklendi; puan paneli `Bakiye: 500 puan` gosterdi, 200 puan inputu readonly karsilik alaninda `20.00 TL` hesaplandi. Tahsilat sonrasi API dogrulama: `totalEarned=630`, `totalSpent=200`, `currentBalance=430`, transactions `Spend 200` + `Earn 130` (`Adisyon #70 - 130 TL`). Snowball yok; nakit/karta odenen net tutar kadar puan kazanildi.

Codex 2026-06-03 B service-session retest: FAIL -> FIX PENDING RESTART. `/Sales` UI'da `Fon` hizmet kartı `20 dk / 100 TL / 10 Seans` ve sepet hint'i `Ödeme alındığında müşteriye 10 seanslık takip açılır` gosterdi; `Codex POS Smoke 20260602` musterisine Nakit 100 TL tahsilat PASS (`Ödeme alındı`, console error yok). Ancak API dogrulama `GET /api/sln-service-sessions/plans?clientId=16&activeOnly=true` bos dondu. Kök sebep: Sales hizmet kartındaki `10 Seans` etiketi gerçek `SlnService.SessionCount` yerine `SlnLoyaltyPackageOffer` bilgisinden geliyordu; `SlnServiceDto`/`SlnServiceCreateDto` da `SessionCount` taşımıyordu. Duzeltme yapildi: DTO + `SlnServiceFactory` create/update/map `SessionCount` tasiyor; `/Services` modalina `Seans sayısı` alani eklendi; Sales kart etiketi ve B takip hint'i artik `service.sessionCount > 1` uzerinden geliyor, A sadakat paketleri sadece `Paket Satışları` alaninda kalıyor. `node --check Sales.js`, `node --check Services.js`, `dotnet build src/CallCenter.Api/CallCenter.Api.csproj -o .codex-build/api-service-session-count-fix` PASS; Salon build ilk denemede VBCSCompiler obj lock'a takildi, `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-service-session-count-fix -p:UseSharedCompilation=false` PASS. API + Salon restart sonrasi `Fon`/uygun hizmet `SessionCount=10` yapilip B plan create + plan session consume retest edilmeli.

Codex 2026-06-03 B service-session final retest: PASS. Restart sonrası `/Services` formunda `Fon` hizmetinin `Seans sayısı` alanı 10 yapıldı; API doğrulama `SlnService id=2 sessionCount=10`. `/Sales` tarafında üstteki A paket teklifi ayrı kaldı (`Fön Paketi`), alttaki normal hizmet kartında `Fon / 20 dk / 100 TL / 10 Seans` göründü. `Codex POS Smoke 20260602` müşterisine Nakit 100 TL satış tamamlandı; sarf guard `Malzeme yok`, personel guard `Devam Et`, toast `Ödeme alındı`. API doğrulama: `SlnServiceSessionPlans id=3`, `sourceInvoiceId=72`, `sourceInvoiceItemId=90`, `totalSessions=10`, `usedSessions=0`, `remainingSessions=10`. Aynı müşteri tekrar seçilince `Aktif Çok Seanslı Hizmet Planları / Fon 10/10 kalan` chip'i göründü; chip sepete `Fon (Plan Seansı #1)` 0 TL olarak eklendi, checkout sonrası API doğrulama `usedSessions=1`, `remainingSessions=9`, `records[0].id=10`, `invoiceId=73`, `invoiceItemId=91`, `sessionNumber=1`.

Codex 2026-06-03 A/B isim ve ekran ayrımı live retest: PASS. `/Services` canlı kontrolde `Seans Takibi` kolonu ve `Seanslı Hizmet 1` istatistiği göründü; `Fon` satırı `10 seans takip`, paket butonu/modalı yok; `Seans / Paket`, `Paket teklifi yok`, `Müşteri Seansları` metinleri yok; console error yok. Sidebar tıklamasıyla `/Sales` açıldı; `Paket Satışları` başlığı var, eski `Seans Satışları` yok, paket kartları üstte ve normal `Fon` hizmet kartı ayrı. `Codex Paket Musteri 772810` autocomplete ile seçildi; sağ panel `Müşteri Paketleri`, `Codex Paket 708903`, `3/12 seans - kalan 10`, `Yüz Ağda`; eski `Müşteri Seansları` yok; console error yok. Sidebar OPERASYON grubu açılarak `/LoyaltyPackages` tıklandı; ekran `Paket Teklifleri`, `Paket Satışları`, `Müşteri Paketleri` gösterdi; eski `Müşteri Seansları`, `Seans Tanımları`, `Seans Planı` yok; console error yok.

Codex 2026-06-03 A loyalty-package Sales usage retest: PASS. `Codex Paket Musteri 772810` seçilince `Müşteri Seansları` panelinde `Codex Paket 708903 / 2/12 seans - kalan 11 / Yüz Ağda` göründü. `Yüz Ağda` hizmeti sepete eklendiğinde kalem `0 TL` oldu ve benefit metni `Codex Paket 708903: satılmış seans planından düşülecek (Kalan 11)` olarak geldi; önceki `undefined` package name kusuru `Sales.js` `offerName/packageName` fallback'i ile düzeltildi. Sarf guard `Malzeme yok`, personel guard `Devam Et` ile checkout tamamlandı. API doğrulama: purchase `id=7`, `usedSessions=2`, `remainingSessions=10`; redemption `id=8`, `invoiceId=74`, `invoiceItemId=92`, notes `Invoice:74|InvoiceNo:SLN-20260603-0009|Service:24`.

### 3.2. Müşteriler (`/Clients` — modül 202)
- [x] Liste, pagination, arama, sıralama
- [x] Müşteri ekle (zorunlu alan validation)
- [x] Detay sayfası: bilgileri, geçmiş randevular, adisyonlar, sadakat bakiyesi
- [ ] Engelli müşteri badge'i (no-show count, blacklisted)
- [x] Şube izolasyonu: Şube Müdürü sadece kendi şubesindeki müşterileri görür

Codex 2026-06-02: `/Clients` liste/arama/ekleme PASS. Liste acildi, pagination tek sayfa olarak gorundu. Arama `Codex POS` ile 1 sonuca dustu. Yeni musteri formunda bos kaydet `Ad soyad ve telefon zorunludur` toast'u verdi; `Codex POS Client 20260602` + `+905553202602` kaydedildi ve listede gorundu. Detay sayfasinda yeni musteri bilgileri ve sadakat/uyelik/seans/hediye karti ozetleri gorundu. POS satis musterisi `Codex POS Smoke 20260602` detayinda Harcamalar sekmesinde `SLN-20260602-0002`, `Sac Kesim`, `150 TL`, `Odendi` gorundu; BUG: ayni detay ust ozetinde `Toplam Harcama 0 TL` kaliyor. UI copy notu: `Dogum Tarihi`, `Tedavi Dosyasi` karakterleri Turkce degil. Screenshot: `.codex-run/screenshots/3-2-clients-initial.png`, `3-2-clients-search.png`, `3-2-clients-new-modal.png`, `3-2-clients-empty-validation.png`, `3-2-clients-saved.png`, `3-2-client-detail.png`, `3-2-client-spend-detail.png`.

Codex 2026-06-03 devam: `/Clients/Detail` toplam harcama bug'i FIX/PENDING API RESTART. Canlı retestte `Codex POS Smoke 20260602` detay üst KPI `Toplam Harcama 0 TL` gösterirken aynı sayfanın Harcamalar tablosunda çok sayıda ödenmiş adisyon (`SLN-20260603-0017`, `100 TL`; `SLN-20260603-0005`, `150 TL`; vb.) listelendi. Kök neden: `SlnClientController` owner rolünde bile JWT `BranchId` claim'ini `branchId` query'sinden önce zorluyordu; owner `Tüm Şubeler` seçmişken client detail istatistiği tek şubeye daralıyor, finance invoice listesi ise role-aware scope ile tüm şubeleri gösteriyordu. Düzeltme: `SlnClientController` branch çözümü `SlnProductController` pattern'ine çekildi (`SalonOwner => claim BranchId scope değil; branch seçilirse query uygulanır; branch-scoped roller claim'e kilitli kalır`). Doğrulama: ilk build çalışan process obj lock'una takıldı, tekrar `dotnet build src\CallCenter.Api\CallCenter.Api.csproj -o .codex-build\api-client-owner-branch-scope-2 -p:UseSharedCompilation=false -m:1` PASS; `dotnet test ... --filter "FullyQualifiedName~SlnClientFactoryTests|FullyQualifiedName~PlatformPhoneLinkingTests"` PASS 13/13. API restart sonrası aynı detay sayfasında KPI tekrar kontrol edilecek.

Codex 2026-06-03 devam: `/Clients/Detail` TR metin fix'i PENDING SALON RESTART. Canlı detayda `Dogum Tarihi`, `Tedavi Dosyasi`, `Seans Kayitlari`, `Henuz seans kaydi yok` gibi bozuk/stale metinler görüldü. Stale DB/cache keylerine takılmamak için görünür müşteri detay etiketleri yeni semantic keylere taşındı: doğum tarihi, tedavi dosyası, sağlık uyarı başlığı, son güncelleme, inceledim, seans kayıtları, yeni seans kaydı, manuel seans kaydı, bakım önerisi, boş randevu/seans durumları ve ilgili JS toast/confirm fallback'leri. `translations-salon.xml` yeni TR/EN keylerle güncellendi. Doğrulama: `node --check src\CallCenter.Salon\wwwroot\js\ClientDetail.js` PASS, `translations-salon.xml` parse PASS, `dotnet build src\CallCenter.Salon\CallCenter.Salon.csproj -o .codex-build\salon-client-detail-text-fix -p:UseSharedCompilation=false -m:1` PASS. Salon restart sonrası canlı metin retest yapılacak.

Codex 2026-06-03 retest: `/Clients/Detail/16` PASS. Restart sonrasi `Codex POS Smoke 20260602` detay sayfasinda ust KPI `Toplam Harcama 1.930 TL`, `Son Ziyaret 03.06.2026` oldu; onceki `Toplam Harcama 0 TL` bug'i canli ekranda kapandi. Bilgiler sekmesinde `Dogum Tarihi` yerine `Doğum Tarihi`, tabda `Tedavi Dosyası` gorundu. Tedavi Dosyası tabina tiklayarak dogrulandi: `Sağlık ve Uyarı Bilgileri`, `Seans Kayıtları`, `Yeni Seans Kaydı`, `Henüz seans kaydı yok.` gorunuyor; eski `Tedavi Dosyasi`, `Seans Kayitlari`, `Henuz seans kaydi yok` metinleri yok. Browser console error yok.

### 3.3. Randevular (`/Appointments` — modül 203)
- [x] Takvim/tarih aralığı görünümü (presetler + liste)
- [x] Boş slot bulma: `GetAvailableSlotsAsync` personel-öncelikli, branch fallback
- [x] Randevu oluşturma (müşteri + personel + hizmet + tarih)
- [ ] Çakışma kontrolü (aynı personel, kesişen saat)
- [ ] Personel çalışma saatleri override edilirse slot filtresi
- [ ] Engelli müşteri kırmızı badge, no-show count sarı badge
- [ ] Status değişimi: Onaylandı → Geldi → Tamamlandı / İptal / No-Show
- [ ] No-Show → ceza/depozito uygulanır mı (NoShowPolicy varsa)

Codex 2026-06-02: `/Appointments` sayfasi PASS (liste/hafta gorunumu render). Mevcut `yeter güleryüz` musterisi autocomplete'de klavye ile secildi; `Sac > Sac Kesim`, `hatice güleryüz`, `2026-06-03` secilince slotlar `09:00`-`18:30` arasi geldi. `09:30` slotu kaydedildi; liste 1 randevudan 2 randevuya cikti ve yeni satir `3 Haz 09:30`, `yeter güleryüz`, `Sac Kesim`, `hatice güleryüz`, `Merkez`, `30 dk`, `Planlanmış / Onay Bekliyor` olarak gorundu. Not: autocomplete dropdown gorsel olarak belirgin acilmadi, ancak ArrowDown+Enter ile secim calisti. Screenshot: `.codex-run/screenshots/3-3-appointments-initial.png`, `3-3-appointment-modal.png`, `3-3-appointment-slot-selected.png`, `3-3-appointment-after-save.png`.

Codex 2026-06-03: Public randevu not gorunurlugu PASS. Kod incelemede public booking notu backend'e kaydediliyor (`dto.Notes -> SlnAppointment.Notes`) ve Salon randevu edit modalina acilinca `form.notes(appt.notes)` ile geliyordu; fakat `/Appointments` listesinde not gorunmuyordu. Hizmet hucresine bekleme listesiyle ayni pattern'de not satiri eklendi (`bi-card-text` + `notes`). `dotnet build src\CallCenter.Salon\CallCenter.Salon.csproj -o .codex-build\salon-appointment-not-visible -p:UseSharedCompilation=false -m:1` PASS. Canli retest PASS: public booking akisiyle yeni musteri `Codex Appt Note 591249` kaydedildi, `koko bostancı` subesi, `Saç Kesim`, `akıra balım`, `03.06.2026 16:00`, not `public appointment note 591249` ile randevu olusturuldu. Salon panelinde menuden `/Appointments` acildi; liste 4 randevuya cikti ve yeni satirda not hizmetin altinda gorundu.

Codex 2026-06-03 devam: `/Appointments` tarih araligi + status click retest PASS/PARTIAL. Menüden sayfa acildi, `Bu Hafta` default 4 randevu ve personel özeti render oldu. `Bugün` ve `Bu Ay` presetleri tiklandi; baslik `Bu Hafta + Bugün + Bu Ay` oldu, cakisan araliklardaki randevular tekillesti (4 satir), console error yok. `Codex Appt Note 591249` satirinda `Onayla` tiklandi: durum `Onaylandı`, `Tamamla` butonu acildi. `Tamamla` tiklandi: durum `Tamamlandı`, `İptal/Gelmedi` kapandi, `Adisyon Oluştur` acildi, personel özeti `akıra balım` tamamlanan sayisi 1 oldu. GAP: test planindaki `Geldi` ara statüsü UI/kod tarafinda yok; mevcut akış `Planlanmış -> Onaylandı -> Tamamlandı` ve alternatif `İptal/Gelmedi` seklinde.

### 3.4. Bekleme Listesi (`/Waitlist` — modül 221)
- [x] Liste, ekleme
- [ ] Slot boşaldığında otomatik bildirim akışı (varsa)

Codex 2026-06-02: `/Waitlist` sayfasi PASS. Bugunun randevulari sekmesi acildi ve `0 randevu` bos durumunu gosterdi. Bekleme Listesi sekmesi bos listeyi ve aramayi gosterdi. `Yeni Kayıt` modalinda bos kaydet `Müşteri, hizmet ve tarih zorunludur` uyarisi verdi. `Codex POS Client 20260602`, `Saç Kesim`, `hatice güleryüz`, `2026-06-04`, `Sabah (09-12)` ile kayit eklendi; tabloda `Bekliyor` durumuyla gorundu. Not alaninda otomatik doldurma denemesi tarayici clipboard kisitina takildi, not zorunlu olmadigi icin akisi etkilemedi. Screenshot: `.codex-run/screenshots/3-4-waitlist-initial.png`, `3-4-waitlist-tab.png`, `3-4-waitlist-modal.png`, `3-4-waitlist-validation.png`, `3-4-waitlist-after-save.png`.

Codex 2026-06-03: `/Waitlist` not akisi PASS. Public profil ve public booking bekleme/randevu not alanlari `value` binding kullandigi icin bazi yazma akislari KO modeline gecmeden payload olusabiliyordu; Salon bekleme listesi tablosu da kaydedilen notu gostermiyordu. Ilk canli retestte public profil modalinda input degerleri gorunmesine ragmen submit `Ad ve telefon zorunlu` dedi; ayni kok nedenle ad/telefon alanlari da KO modeline gecmiyordu. Duzeltildi: public profil/book waitlist ad/telefon ve public booking ad/e-posta alanlari `textInput`; public waitlist/randevu not textarea'lari ve Salon waitlist/convert not textarea'lari `textInput`; Salon bekleme listesi hizmet hucresinde not gorunur; local arama notu da filtreler. Backend zaten `dto.Notes` -> `SlnWaitlistEntry.Notes` / `SlnAppointment.Notes` map ediyordu. `dotnet build src\CallCenter.Salon\CallCenter.Salon.csproj -o .codex-build\salon-public-waitlist-inputs-fix -p:UseSharedCompilation=false -m:1` PASS; `dotnet test ... --filter "FullyQualifiedName~SlnWaitlistFactoryTests"` PASS 21/21. Canli retest PASS: public profil modalindan `Codex Public Note 742622`, telefon `90555742622`, hizmet `Saç Kesim`, not `public waitlist note 742622` ile kayit olusturuldu; public toast basarili geldi. Salon panelinde menuden `/Waitlist` acildi, sayac `Bekleme Listesi 3` oldu ve yeni satirda not hizmetin altinda gorundu.

Codex 2026-06-03 devam: `/Waitlist` manuel bildirim PASS, otomatik slot bildirimi GAP. Menüden sayfa acildi, `Bekleme Listesi` sekmesinde `Codex Public Note 742622` satiri `public waitlist note 742622` notuyla gorundu. `Bildir` butonu tiklandi; satir durumu `Bildirildi` oldu, `Bildir` butonu kayboldu, `Randevuya Dönüştür` ve `Randevu Alindi (Manuel)` aksiyonlari kaldı, console error yok. Kod taramasinda slot bosalinca kendiliginden bildirim gonderen worker/trigger bulunmadi; mevcut sistem manuel `Bildirildi` statüsüyle ilerliyor.

### 3.5. Hizmetler / Reçeteler / Personel Fiyatları
- [x] `/Services` (204): kategori CRUD, hizmet CRUD, multi-session sessionCount
- [x] `/Recipes` (215): reçete tanımı, malzeme + miktar
- [x] `/PersonnelPrices` (228): personel × hizmet override fiyatlar
- [ ] `/Staff` (209): personel CRUD, çalışma saatleri (workingDays + override)

Codex 2026-06-03: 3.5 kismi PARCALI. `/Services` liste PASS: 32 aktif hizmet, 6 kategori, `Lazer Epilasyon` ve `Seans Tanımı` sayaci gorundu; `Yeni Hizmet` modalinda ad/kategori/sure/fiyat/durum ve `Randevu ve kaynak ayarları` panelinde ana hizmet, ek hizmet, patch test, hazirlik/islem/toparlama ve kaynak ihtiyaci alanlari gorundu. Seans tanimi modalinda `Fon Paketi` icin seans sayisi/fiyat/gecerlilik formu gorundu, veri degistirilmedi. `/Recipes` form/validation PARTIAL: modal alanlari ve ikon duzeni PASS, bos kaydet `Reçete adı zorunludur` uyarisi verdi; `sac boyası` + miktar 5 secildiginde ekranda ad dolu gorunmesine ragmen eski build'de kaydet yine `Reçete adı zorunludur` dedi. View binding'i `textInput` olarak duzeltildi; build/restart sonrasi retest gerekiyor. `/PersonnelPrices` PASS: bos kaydet validation goruldu, `hatice güleryüz x Cilt Bakımı = 333 TL` override kaydi listeye dustu. `/Staff` PARTIAL: liste, yeni personel formu, hizmet yetenekleri, site gorunurluk ayarlari ve password policy validation PASS; personel olusturma `Maksimum personel limitine ulaşıldı` limitiyle durdu. Vardiya kaydi PASS: `akıra balım`, `03.06.2026`, `09:00 - 18:00`, mola `60` satiri olustu. Screenshot: `.codex-run/screenshots/3-5-services-initial.png`, `3-5-services-new-modal.png`, `3-5-services-settings-panel.png`, `3-5-services-session-modal.png`, `3-5-recipes-modal.png`, `3-5-recipes-resave-after-restart.png`, `3-5-personnel-prices-after-save.png`, `3-5-staff-validation.png`, `3-5-staff-shift-after-save.png`.

Codex 2026-06-03 retest: `/Recipes` kaydetme PASS. `textInput` binding duzeltmesi sonrasi `Codex Recete Pass 20260603` recetesi `sac boyası`, miktar `5gr`, maliyet `500,00 TL` ile kaydedildi ve listede gorundu. Screenshot: `.codex-run/screenshots/3-5-recipes-after-binding-modal.png`, `3-5-recipes-after-binding-filled.png`, `3-5-recipes-after-binding-save.png`.

Codex 2026-06-03 retest: `/Services` CRUD + multi-session PASS. Sayfa menuden acildi; async yukleme tamamlaninca 32/32 aktif hizmet, 7 kategori ve `Seansli Hizmet 1` gorundu, console error yok. Tarayici input yazma katmani lokal ortamda `Browser Use virtual clipboard is not installed` hatasina dustugu icin create/update/delete yazimlari API ile yapildi, UI render ve menu akisi browser'da dogrulandi. Test kaydi: `Codex Hizmet Kategori 094900` (id 62) ve `Codex Cok Seansli Hizmet 094900` (id 326) `sessionCount=3` ile olusturuldu; `/Services` reload sonrasi `45 dk / 3 seans takip` ve 33/33 aktif hizmet gorundu. Update sonrasi `duration=50`, `price=333`, `sessionCount=4`; UI'da `50 dk / 4 seans takip` gorundu ve eski 3 seans satiri kayboldu. Ardindan test hizmeti ve bos kategori silindi; reload sonrasi test kaydi yok, sayaclar 32/32 ve 7 kategoriye dondu, console error yok.

Codex 2026-06-03 retest: `/Staff` PARTIAL/BLOCKED BY DATA. Sayfa menuden acildi; 6 aktif personel, rol/sube kolonlari ve vardiya satiri render oldu, console error yok. `akira` satirinda edit modal acildi; sifre bos birakilarak degisikliksiz `Kaydet` tiklandi, modal kapandi ve satir `koko bostanci / Sube Muduru / Aktif` olarak korundu, console error yok. Yeni personel create API denemesi 400 ile beklenen limite takildi: `Maksimum kullanici limitine (5) ulasildi.` Bu nedenle full create/delete CRUD bu hesapta kosulamadi; limit artirilir veya test hesabina ek kullanici hakki verilirse yeniden denenmeli.

### 3.6. Stok (`/Products` 205, `/Suppliers` 210)
- [x] Ürün CRUD, stok seviyesi
- [x] Tedarikçi CRUD, cari bakiye
- [x] Düşük stok alert (varsa)
- [x] Reçete tüketimi sonrası stok eksilir (Sales akışından)

Codex 2026-06-03: `/Products` + `/Suppliers` PASS. `Codex Stok Smoke 307748` urunu `codex kategori/codex marka` ile kaydedildi; stok 7, min stok 10 oldugu icin kritik stok kartinda gorundu. `Codex Tedarikci 422065` tedarikcisi kaydedildi. Ilk tedarik siparisi kaydi `SlnSupplierOrders.CreatedByPersonnelId` FK hatasiyla durdu; sebep controller'in `CustomerPersonnelId` yerine platform `UserId` gecmesiydi. `SlnProductController` ve `SlnProductFactory` personel FK akisi duzeltildi. Restart sonrasi siparis `SO-20260602-001` olarak olustu, `Onayla` modalinda teslim alinip stoklara islendi; tedarikci bakiyesi `1.443 TL`, urun stogu `7 -> 20` oldu ve kritik stok uyarisi kalkti. Sales recete tuketimi PASS: `Sac Boyama` hizmetinde `1 sarf` otomatik geldi; `Tüm Şubeler` seciliyken hızlı musteri `Codex Sarf All 607506` ile Nakit 400 TL tahsilat tamamlandi, toast `Ödeme alındı`, console error yok, `sac boyası` stogu `45 -> 40` dustu. Branch scope retest PASS: `/proxy/sln-products?branchId=5` artik `sac boyası` dondurmuyor (`count=0`); Sales'te `koko bostancı` seciliyken `boya` aramasi sonuc getirmedi, `Tüm Şubeler` seciliyken `sac boyası 200 TL Stok 40` gorundu, console error yok.

### 3.7. Finans
- [x] `/Invoices` (206): adisyon listesi, detay, iptal
- [x] `/Cash` (207): kasa hareketleri, gün sonu kapama, açık kasa kontrolü
- [x] `/Expenses` (208): masraf CRUD, kategori
- [x] `/GiftCards` (216): kart oluşturma, bakiye sorgulama, satın alma akışı, harcama
- [x] **Sadakat Paketleri (`/LoyaltyPackages` 217 — Operasyon grubunda)**:
  - Paket tanımla (10 öde 12 al gibi)
  - Müşteri satın alma → kredi bakiyesi (`SlnLoyaltyPackagePurchase`)
  - Adisyondan kullanım → kredi düşer
  - Bakiye sıfırlanınca expire / pasif

Codex 2026-06-03: `/Invoices` liste + detay PASS. `SLN-20260602-0003` adisyonu `Codex Sarf All 607506 / Sac Boyama / 400 TL / Nakit / Odendi` olarak listede gorundu; detay modalinda hizmet satiri, KDV ve toplam tutar dogru acildi, console error yok. Iptal akisi PARTIAL: ekranda acik adisyon olmadigi icin UI iptal butonu test edilemedi, odendi adisyonu zorla iptal edilmedi. `/Cash` PASS. Ana Kasa hareketlerinde `Adisyon: SLN-20260602-0003` 400 TL gelir olarak gorundu; Z Raporu modalinda nakit gelir toplami acildi; Gun Sonu modalinda Gelir/Gider/Sistem Net ve sayilan tutar alanlari render oldu, kapama kaydi yazilmadi. UI copy notu: Gun Sonu modalinda `Gun Sonu` ve `Sayilan` Turkce karakterleri duzeltilmeli.

Codex 2026-06-03: `/Expenses` ilk test FAIL/PENDING RETEST. Yeni masraf modalinda DOM alanlari dolu olmasina ragmen KO model eski kaldigi icin `Aciklama ve tutar zorunludur` uyarisi geldi; kategori filtresinde ayni kategori onlarca kez gorundu. Duzeltme yapildi: aciklama/tutar `textInput`, kategori listesi isim bazli tekil, `Tum Kategoriler/Kategori yazin/Odeme Yontemi` ve Cash `Gun Sonu/Sayilan/Kasa Ac` metinleri duzeltildi. `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-expenses-fix` PASS. VS restart sonrasi `/Expenses` yeniden test edilmeli.

Codex 2026-06-03 retest: `/Expenses` PASS. Sayfa acildi, kategori filtresi tekil gorundu, console error yok. `Yeni Masraf` modalinda `Kira / Codex expense retest 480506 / 123 TL / Nakit` kaydedildi; liste 3 kayda cikti, toplam `16.123 TL`, bu ay `123 TL`, toast `Masraf eklendi`. Onay akisi PASS: Bootstrap confirm modal `Bu masrafı onaylamak istediğinize emin misiniz?` acildi, `Onayla` sonrasi durum `Onaylı`, toast `Masraf onaylandi`, console error yok. `/Cash` -> Ana Kasa -> Hareketler PASS: `03.06.2026 Gider Masraf #3: Codex expense retest 480506 123 TL` satiri gorundu.

Codex 2026-06-03: `/GiftCards` PASS. `/GiftCards` Salon'dan CRM `/SalonCrm/GiftCards` akisana yonlendi; `codexkokobuyer` test hesabi ile CRM login yapildi. CRM'de `Yeni Hediye Karti -> Kaydet` ile `GC-B39DB4A637E2` olustu, 100 TL bakiye listede gorundu. Salon `/Sales` tarafinda `Yuz Agda` 50 TL sepete eklendi, odeme yontemi `Hediye Karti`, kod `GC-B39DB4A637E2`; sarf guard'inda `Malzeme yok`, hizli musteri ve personelsiz tahsilat guard'lari gecildi. Sepet temizlendi; API dogrulamasi kart bakiyesi `100 -> 50`, hareket `Adisyon #64` olarak yazildi, console error yok.

Codex 2026-06-03: `/LoyaltyPackages` PASS/PENDING VISUAL RETEST. Operasyon menusu altindaki sayfadan `Codex Paket 708903` teklif tanimi olusturuldu; `/Sales` tarafinda `Seans Satislari` alaninda gorundu ve `Codex Paket Musteri 772810` icin Nakit 80 TL tahsilatla satildi. API dogrulamasi: `SlnLoyaltyPackagePurchase` id `7`, `totalSessions=12`, `usedSessions=0`, `remainingSessions=12`, aktif. `/LoyaltyPackages` ekraninda `1 seans dus` akisi calisti; toast geldi, API/redemption dogrulamasi sonrasi `usedSessions=1`, `remainingSessions=11`, redemption id `7`, not `Manuel seans kullanimi`. UI bug bulundu ve duzeltildi: view `packageName` bekliyordu ama DTO `offerName` donuyor; bu nedenle musteri seanslari tablosunda plan/hizmet/kalan kolonlari bos ve detay basligi `undefined · Yuz Agda` gorunuyordu. `LoyaltyPackages` binding ve Sales/Loyalty fallback Turkce metinleri duzeltildi. `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-loyaltypackages-fix` PASS. VS restart sonrasi `/LoyaltyPackages` gorsel retest edilmeli.

Codex 2026-06-03 retest: `/LoyaltyPackages` PASS. JS parse hatasi bulundu: `0'dan` fallback string'i tek tirnak icinde oldugu icin `LoyaltyPackages.js` hic calismiyor, KO binding uygulanmiyor ve tablo ham template gibi bos satir + `Kullan` gosteriyordu. String cift tirnaga alindi; `node --check src/CallCenter.Salon/wwwroot/js/LoyaltyPackages.js` PASS, `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-loyaltypackages-js-fix` PASS. Restart sonrasi satir dolu geldi: `Codex Paket Musteri 772810 / Codex Paket 708903 / Yuz Agda / 1/12 / kalan 11`; `1 seans dus` modalinda ayni veriler dogru gorundu; console error yok. Seans dusme onaylanmadi.

### 3.8. Müşteri İlişkileri (Marketing Composite — `/Marketing`)
- [x] Kampanyalar (SMS, 212)
- [x] Üyelik Planları (218) — satış akışı, indirim/seans paketi
- [x] Hediye Kartları (216)
- [x] E-posta Kampanyaları (222)
- [x] Yorumlar (223)
- [x] Geri Kazanım (227)
- [x] **Sadakat Programı tab YOK** (CRM'e taşındı)
- [x] **Sadakat sekmesi yok** (CRM'e taşındı)

Codex 2026-06-03: `/Marketing` Salon icinden CRM `/SalonCrm/Campaigns` sayfasina yonleniyor; Salon icinde Sadakat/Sadakat Programi tablari gorunmedi, console error yok. CRM Campaigns SMS tabinda mevcut taslak listelendi, segment presetleri calisti (`Tum aktif musteriler`: hedef 11, SMS 4, telefon eksik 7, maliyet 3,20 TL). Browser otomasyonunda modal alanlarinin DOM degeri doldugu halde KO modeline gecmedigi icin Kaydet `Ad ve mesaj zorunludur` uyarisi verdi; onceki Expenses sorunu ile ayni `value` binding pattern'i. Duzeltme yapildi: CRM Campaigns form metin/sayi alanlari `textInput` kullanacak sekilde guncellendi (`Campaigns.cshtml`). `dotnet build src/CallCenter.Crm/CallCenter.Crm.csproj -o .codex-build/crm-campaigns-textinput-fix` PASS. CRM restart sonrasi SMS kampanya Kaydet retest edilmeli.

Codex 2026-06-03 retest: CRM Salon menuleri PASS smoke. `codexkokobuyer` ile CRM login yapildi; `/Home/Salon` acildi. Sidebar linkleri tiklanarak `Sadakat`, `Uyelikler`, `Hediye Kartlari`, `Pazarlama ve SMS`, `E-posta Kampanyalari`, `Yorum Yonetimi`, `Kayip Musteri` sayfalari acildi; tumunde baslik/liste veya bos durum render oldu, console error yok. CRM Campaigns `textInput` fix retest PASS: `Pazarlama ve SMS > Yeni` modalinda `Codex SMS Press 100200` adi ve `Merhaba bu bir Codex test mesajidir` mesaji yazildi, `Tum aktif musteriler` segmenti secilince hedef `12`, SMS `5`, telefon eksik `7`, maliyet `4,00 TL` oldu; Kaydet sonrasi modal kapandi, listeye `Codex SMS Press 100200 / Taslak / 5 / 0` satiri dustu ve `Kampanya kaydedildi` toast'u geldi, console error yok.

### 3.9. Yönetim (Salon Owner only)
- [x] `/Profile` (220): salon profili, slug, çalışma saatleri
- [x] `/Branches` (213): şube CRUD, branch slug, default branch
- [x] `/NoShowPolicy` (224): politika tanımı, ceza/depozito kuralı
- [x] `/ConsentForms` (225): form CRUD, müşteri imzası
- [ ] `/BeforeAfter` (226): fotoğraf yükleme, müşteri × hizmet
- [x] `/EmailSettings`: SMTP/OAuth (Gmail/Outlook) ayarları
- [x] `/PageSettings`: public salon sayfası ayarları
- [x] `/PaymentInfo`: iyzico sub-merchant onboarding (PS.5)
- [x] `/DataImport`: eski salon verisi importu

Codex 2026-06-03: 3.9 yonetim turu PARCALI/PENDING RETEST. `/Profile` PASS: profil sayfasi acildi, no-change kaydet toast `Profil kaydedildi`, console error yok. `/Branches` PASS smoke: `koko bostancı` ve `Merkez` subeleri listelendi, console temiz; CRUD yazilmadi. `/ConsentForms` PASS smoke: mevcut `Lazer Epilasyon Onam Formu`, yeni form modalinda hazir sablonlar ve Word butonu gorundu; in-app browser download event'i yakalayamadigi icin dosya indirme manuel dogrulama bekliyor. UI duzeltmeleri yapildi: `Form Tanımları`, `İmza`, `İmza Tarihi` metinleri ve form alanlari `textInput`. `/NoShowPolicy` PASS smoke: sayfa acildi, no-change kaydet toast verdi; number/text alanlari `textInput`, `No-Show` metinleri ve TR fallback'leri duzeltildi. `/EmailSettings` PASS smoke: sayfa acildi, hesap listesi bos durum ve butonlar render oldu, console error yok; `Diger SMTP` -> `Diğer SMTP` ve Gmail/Yandex/SMTP/test alanlari `textInput` yapildi. `/DataImport` sidebar/controller/view fallback metinleri `Veri Aktarımı` olarak duzeltildi. `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-ui-text-retouch` PASS. VS restart + hard refresh sonrasi bu UI metin/form retestleri tekrar bakilacak.

Codex 2026-06-03 retest/fix: `/EmailSettings` PASS: `Diğer SMTP` gorundu, SMTP modal acildi, Gmail/Yandex/SMTP/test inputlari `textInput`, console error yok. `/NoShowPolicy` PASS function: kaydet toast verdi, console error yok; canli ekranda bazi karaktersiz metinlerin DB/cache translation kaydindan geldigi goruldu. `/ConsentForms` FAIL text: canli DB/cache eski `Form Tanimlari / Imzalayan / Imza Tarihi` key degerlerini basiyor; gorunen yerler yeni semantic keylere tasindi (`salon.consentforms.form_definitions`, `signer`, `signature_date`) ve XML'e eklendi. `/DataImport` PARTIAL text: ana view metinleri duzeldi, template display name'leri API'den `Musteriler / Urunler ve Stok` geliyordu; `SlnDataImportFactory` template adlari/kolonlari/ornek satirlari ve import sonuc mesajlari Turkce karakterli hale getirildi. `DataImport.js` fallback toast/status metinleri duzeltildi. `node --check src/CallCenter.Salon/wwwroot/js/DataImport.js` PASS. `dotnet build src/CallCenter.Api/CallCenter.Api.csproj -o .codex-build/api-dataimport-text-fix` PASS. `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-dataimport-consent-text-fix` PASS. API + Salon restart sonrasi `/ConsentForms` ve `/DataImport` metin retestleri tekrar kosulacak.

Codex 2026-06-03 retest: `/ConsentForms` PASS text/function smoke. Restart sonrasi `Form Tanımları`, `İmzalayan`, `İmza Tarihi`, `İmzalanan Formlar` dogru gorundu; table text temiz, console error yok. `/DataImport` PARTIAL/PENDING SIDEBAR RETEST: ana icerik PASS (`Eski Veri Aktarımı`, `Excel şablonu indir`, `1. Veri tipini seç`, `Müşteriler`, `Ürünler ve Stok`, `Varsayılan Şube`, `Önizle`, `Uygun satırları aktar`, `Hazır`, `Aktarılan`, `Satırlar`, `Henüz önizleme yapılmadı.`), console error yok. Kalan tek bozuk metin sidebar `Veri Aktarimi`; eski DB/cache key'ini bypass etmek icin layout `salon.sidebar.data_import_label` yeni key'e tasindi ve XML'e eklendi. `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-dataimport-sidebar-text-fix` PASS. Salon restart sonrasi sidebar retest edilecek.

Codex 2026-06-03 retest: `/DataImport` PASS. Restart sonrasi sidebar ve sayfa basligi `Veri Aktarımı` olarak geldi, eski `Veri Aktarimi` gorunmedi. Ana icerikte `Eski Veri Aktarımı`, `Müşteriler`, `Ürünler ve Stok`, `Varsayılan Şube`, `Önizle`, `Hazır`, `Aktarılan` metinleri dogru; console error yok.

Codex 2026-06-03 devam: `/PageSettings` PASS smoke/function. Sayfa acildi, profil/booking linkleri, logo-favicon-kapak-galeri-bolumler ve reklam gorselleri render oldu; profil linki kopyalama toast'u `Profil linki kopyalandı.`, reklam gorseli satiri ekle/sil UI akisi console errorsiz calisti. `/PaymentInfo` PASS validation smoke: iyzico Pazaryeri formu acildi, bos submit kayit yapmadan `IBAN zorunlu.` uyarisi verdi, console error yok. `/BeforeAfter` FULL TEST BLOCKED: hesapta modul 226 aktif olmadigi icin `/ModuleRequired?moduleId=226` ekranina dusuyor. Bu ekranda hard-coded `Bu hizmet aktif degil / Bu ekrani kullanmak icin...` metinleri bulundu; `ModuleRequired` view'i translation key'leriyle Turkce hale getirildi, XML eklendi. `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-modulerequired-text-fix` PASS. Salon restart sonrasi ModuleRequired metin retest ve modul acilirsa BeforeAfter gercek sayfa testi yapilacak.

Codex 2026-06-03 retest: `/BeforeAfter` module gate PASS. Restart sonrasi `/BeforeAfter` -> `/ModuleRequired?moduleId=226...` redirect verdi; `Bu hizmet aktif değil` ve `Bu ekranı kullanmak için hizmeti satın alın` metinleri dogru, eski ASCII bozuk metinler yok, console error yok. BeforeAfter gercek CRUD testi modul 226 aktif edilince yapilacak.

### 3.10. Raporlar (`/Reports` — 211)
- [ ] Günlük/aylık ciro
- [ ] Personel hak ediş (settlement)
- [ ] Hizmet popülerlik
- [ ] Müşteri retansiyon

Codex 2026-06-03: `/Reports` FULL TEST BLOCKED. Hesapta modul 211 aktif olmadigi icin `/ModuleRequired?moduleId=211` ekranina yonlendi; rapor canvas/tablo icerigi acilmadi. ModuleRequired metin bozuklugu 3.9 notundaki fix ile kaynakta duzeltildi ve build PASS. Rapor akisi icin modul satin alma/aktivasyon sonrasi tekrar test edilmeli.

Codex 2026-06-03 retest: `/Reports` module gate PASS. Restart sonrasi `/Reports` -> `/ModuleRequired?moduleId=211...` redirect verdi; `Bu hizmet aktif değil` ve `Bu ekranı kullanmak için hizmeti satın alın` metinleri dogru, eski `aktif degil/ekrani/satin alin` metinleri yok, console error yok. Rapor sayfasi 211 modul aktif edilince test edilecek.

### 3.11. Public Salon (Anonim akış)
- [x] `/s/{slug}` profile sayfası
- [x] Randevu alma: hizmet/personel/tarih seçimi, deposit (varsa)
- [ ] Üyelik satın alma (Iyzico checkout)
- [ ] ServiceCombos / kombo paketler — mobil görünüm
- [ ] Public proxy CSRF/XSS koruma

Codex 2026-06-03: Public Salon click-test PARTIAL. Direkt public URL yerine tek tabda `/Home -> Yönetim -> Sayfa Ayarları -> profil linki` tıklandı; PageSettings public linkleri `target="_blank"` kullandığı için in-app browser'da önce yeni tab/popup karışıklığı yarattı. Linkler aynı sekmede açılacak şekilde düzeltildi; `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-pagesettings-same-tab-links` PASS. Restart sonrası aynı tıklama akışı PASS: `/salon/koko-guzellik-merkezi` açıldı, console error yok. Public profil smoke: hizmet kategorileri, üyelik planı, iletişim ve paylaşım alanları render oldu. `Online Randevu Al` tıklaması aynı tabda `/user/login?returnUrl=/salon/koko-guzellik-merkezi/book` ekranına gitti; müşteri login zorunluluğu beklenen davranış, console error yok. `Bekleme Listesine Ekle` modalı açıldı; ad/telefon/saat/hizmet/tarih/not alanları ve hizmet listesi geldi, kapandı, console error yok. Üyelik `Üye Ol` modalı açıldı; ad/telefon/e-posta ve `Ödeme Formunu Aç` görüldü; ödeme başlatılmadı. UI/text fix: public profil paylaşım alanında `Paylasim`, `Profili Paylas`, `masaustu afisleri icin`, `Farketmez`, kopyalama toast fallback'leri ve üyelik modalında görünür `Vazgeç` eksikliği düzeltildi. `node --check src/CallCenter.Salon/wwwroot/js/PublicProfile.js` PASS, `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-public-profile-text-fix` PASS. Salon restart sonrası public metin retest edilecek. Not: `ARADIGINIZ HERSEY BURADA` kodda bulunmadı; canlı profil açıklaması verisinden geliyor.

Codex 2026-06-03 retest: Public Salon PASS. XML/restart sonrası eski DB/cache translation değerleri hâlâ `Paylasim/masaustu` basınca görünür yerler yeni semantic key'lere taşındı (`title_label`, `share_label`, `qr_hint_label`, `*_copied_label`, `any_time_label`); `node --check PublicProfile.js` PASS, `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-public-profile-new-keys` PASS. Restart + XML yükleme sonrası `/salon/koko-guzellik-merkezi` tek tab retest: `Paylaşım ve QR`, `Profili Paylaş`, `Instagram, Google ve masaüstü afişleri için` doğru; bad text match yok, console error yok. Bekleme listesi modalı PASS: `Fark etmez` doğru, `Farketmez` yok, hizmet listesi geldi, console temiz. Üyelik modalı PASS: `Vazgeç` ve `Ödeme Formunu Aç` görünür, ödeme başlatılmadı, console temiz. Kalan veri notu: `ARADIGINIZ HERSEY BURADA` profil açıklaması alanından geliyor; Salon Profili verisi olarak düzeltilmeli.

Codex 2026-06-03 devam: Public booking/customer flow PARTIAL. Tek tabda public landing `Randevu Al` -> `/discover` -> `koko güzellik merkezi` profil kartı -> `Online Randevu Al` akışı tıklanarak ilerletildi. Booking URL doğrudan anonim form açmıyor; `/user/login?returnUrl=/salon/koko-guzellik-merkezi/book` ekranına yönlendiriyor. Public müşteri kaydı PASS: `Codex Public 524969`, telefon `+905555249690`, KVKK onayı ile kayıt sonrası booking wizard açıldı. Hizmet seçimi PASS: `Saç Kesim` ve `Fon` seçilince `İleri` açıldı. Randevu oluşturma BLOCKED/DATA: personel adımında `Bu hizmet için uygun personel bulunamadı`; public endpoint kontrolünde örnek hizmetler `1,2,3,9,13,21,117,118` için `/available-staff` hep `[]` döndü. Profil datasında `showTeam=true` olmasına rağmen ekip listesi dönmüyor. Bekleme listesi fallback PASS: form ad/telefonu otomatik doldurdu ve `Bekleme listesine eklendiniz. Slot bosaldiginda salon sizinle iletisime gececek.` başarı mesajı geldi, console error yok. Sonuç: public profil/discover/register/waitlist çalışıyor; gerçek randevu ve Salon tarafında görünürlük testi için bu salonda public görünür/şube kapsamına uygun personel-hizmet ataması gerekiyor.

Codex 2026-06-03 final retest referansi: Public randevu alma PASS/PARTIAL. `/salon/koko-guzellik-merkezi/book` ekraninda sube secici geldi; `koko bostancı` secilince personel listesi (`Fark Etmez`, `akıra balım`, `mualla çöpek`, `sukellamudur`) ve gerçek slotlar geldi. `Saç Kesim`, `14:30`, public musteri `Codex Public 524969` ile `Randevunuz Oluşturuldu!` goruldu. Salon `/Appointments` tarafinda `3 Haz 14:30 / Codex Public 524969 / Saç Kesim / akıra balım / koko bostancı / Planlanmış Onay Bekliyor` satiri dogrulandi. Deposit bu veri/policy'de devrede olmadigi icin ödeme/depozito adimi kosulmadi.

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

Codex 2026-06-03: 3.12 rol matrisi browser retest PASS/PARTIAL. Test hazirligi: mevcut `codexkokobuyer` owner sifresi local test icin `OwnerRole2026!` yapildi; `sukella`, `mualla` ve gecici rol testinde kullanilan `akıra` sifresi `RoleTest2026!` yapildi. Yeni personel acma UI denendi ama `Maksimum personel limitine ulasildi` uyarisi dogru geldi; bu nedenle mevcut `akıra` kaydi rol testlerinde sirayla kullanilip sonunda eski `Ekip Lideri`/branch null durumuna geri alindi.

Rol sonuclari: 102 Mudur (`sukella`) login PASS; `/Services` acildi, `/Profile` Home'a dustu, console temiz. 103 Kuafor (`akıra` gecici) PASS; `/Recipes` acildi, `/Services` Home'a dustu, console temiz. 104 Guzellik Uzmani (`akıra` gecici) PASS; `/Recipes` acildi, `/Products` Home'a dustu, console temiz. 105 Kasiyer (`akıra` gecici) PASS; `/Cash` acildi, `/Services` Home'a dustu, console temiz. 106 Resepsiyonist (`akıra` gecici) PASS; `/Appointments` acildi, `/Cash` Home'a dustu, console temiz. 107 Sube Muduru (`akıra` gecici) PASS; JWT BranchId=5 geldi, `/Staff` sadece branch 5 personelini gosterdi, `/Branches` Home'a dustu, `/Clients` branch 5 musteri listesiyle acildi, console temiz. Not: Salon CRM/Musteri Iliskileri menusu bu surumde Salon'dan kaldirildigi icin 3.12'deki eski Marketing/GiftCards beklentileri yeni mimariye gore CRM tarafinda test edilmeli.

### 3.13. Modül Bazlı Engel
- [ ] Modülü olmayan müşteri o sayfaya gidince `/ModuleRequired?moduleId=X` redirect
- [ ] Müdür panelinden modül satın alma → tekrar dener → sayfa açılır
- [ ] JWT refresh sonrası yeni modüller claim'e yazılmış olmalı

Codex 2026-06-03: module gate kismi PASS/PARTIAL. 226 (`/BeforeAfter`) ve 211 (`/Reports`) aktif olmayan moduller icin `/ModuleRequired?moduleId=X` redirect dogru calisti; ModuleRequired metinleri Turkce karakterli ve console temiz. Satin alma -> JWT refresh -> sayfa acilir akisi henuz kosulmadi.

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
- [x] `/Home/Salon` veya `/Home` salon vertical dashboard'a düşer
- [x] Sidebar "Salon" grubu açık, link'ler:
  - Sadakat → `/SalonCrm/Loyalty`
  - Üyelikler → `/SalonCrm/Memberships`
  - Hediye Kartları → `/SalonCrm/GiftCards`
  - Kampanyalar → `/SalonCrm/Campaigns`
  - E-posta Kampanyaları → `/SalonCrm/EmailCampaigns`
  - Yorumlar → `/SalonCrm/Reviews`
  - Kayıp Müşteri → `/SalonCrm/Winback`

**SalonCrm/Loyalty (2 tab)**
- [x] Sadakat Puanı tab (C):
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

Codex 2026-06-03: CRM Salon scope navigation PASS. `codexkokobuyer` ile CRM login sonrasi sidebar Salon grubu acik gorundu: Sadakat, Uyelikler, Hediye Kartlari, Pazarlama ve SMS, E-posta Kampanyalari, Yorum Yonetimi, Kayip Musteri linkleri dogru route'lara gidiyor. `/SalonCrm/Memberships` mevcut `Codex Uyelik 20260602223717` planini ve `Codex CRM Compat 20260602223717` aktif musteri uyeligini gosterdi; bu uyelik daha once Salon `/Sales` akisisinda otomatik avantaj olarak calisti. `/SalonCrm/GiftCards` daha once `GC-B39DB4A637E2` olusturma + Salon `/Sales` harcama ile PASS. `/SalonCrm/Campaigns`, `/SalonCrm/EmailCampaigns`, `/SalonCrm/Winback` mevcut taslak/kural kayitlarini listeledi ve segment presetleri dogru sayilari verdi; `/SalonCrm/Reviews` acildi ama test verisinde yorum yoktu. Adapter dogrulamasi: API ile `Codex SMS API 021319` kampanyasi olusturuldu (`totalRecipients=4`) ve CRM listesinde gorundu; `Codex Email API 021343` e-posta kampanyasi olusturuldu (`totalRecipients=2`) ve listede gorundu; `Codex Winback API 021422` kurali olusturuldu (`45 gun, SMS, %3, aktif`) ve listede gorundu. Form input retest notu: Campaigns, EmailCampaigns, Memberships, GiftCards, Winback ve Loyalty config text/number alanlari `textInput` pattern'ine cekildi. `dotnet build src/CallCenter.Crm/CallCenter.Crm.csproj -o .codex-build/crm-saloncrn-textinput-fix` PASS. CRM restart sonrasi CRUD kaydet retestleri tekrarlanacak.

Codex 2026-06-03 retest: CRM Salon sidebar smoke PASS. Salon app switcher'a tiklanarak CRM login'e gecildi; `codexkokobuyer` / `OwnerRole2026!` ile login PASS ve `/Home/Salon` acildi. Sidebar'da Salon grubu altinda `Sadakat`, `Uyelikler`, `Hediye Kartlari`, `Pazarlama ve SMS`, `E-posta Kampanyalari`, `Yorum Yonetimi`, `Kayip Musteri` gorundu; Core/CallCenter gruplari gorunmedi. Sidebar linkleriyle tiklama smoke: `/SalonCrm/Loyalty`, `/SalonCrm/Memberships`, `/SalonCrm/GiftCards`, `/SalonCrm/Campaigns`, `/SalonCrm/EmailCampaigns`, `/SalonCrm/Reviews`, `/SalonCrm/Winback` hepsi acildi, login'e dusme yok, console error yok. Bu tur CRUD degil; CRUD/adapter derin testleri ayni bolumde acik kaliyor.

Codex 2026-06-03 CRUD retest: CRM Salon create/save akislari PASS. `/SalonCrm/Winback`: `Codex UI Winback 750213` kuralı kaydedildi, listede gorundu, toast `Geri kazanım planı kaydedildi`, console temiz. `/SalonCrm/Campaigns`: `Codex UI SMS 798173` taslagi kaydedildi, hedef 4, toast `Kampanya kaydedildi`, console temiz. `/SalonCrm/EmailCampaigns`: `Codex UI Email 836895` taslagi kaydedildi, hedef 2, toast `E-posta kampanyası kaydedildi`, console temiz. `/SalonCrm/GiftCards`: `Codex Gift UI 870424` alicili 88 TL kart olustu (`GC-90A3ECA6B352`), listede gorundu, console temiz. `/SalonCrm/Memberships`: `Codex UI Uyelik 925331` planı 144 TL / %6 / 45 gun / Sac Kesim ile kaydedildi, listede gorundu, console temiz. `/SalonCrm/Loyalty`: puan config no-change kaydet PASS (`Sadakat ayarları kaydedildi`); `Codex UI Program 994407` Manikur 3 ziyaret -> Manikur odul programi kaydedildi, listede gorundu, console temiz. `/SalonCrm/Reviews` create akisi yok; sadece liste/empty-state smoke PASS, veri gelince onay/red CRUD test edilmeli.

### 4.3. CRM CallCenterCrm Scope (modüller 501-506)
CallCenter CRM paketi satın alınmış kullanıcıyla:
- [ ] `/Home/CallCenter` dashboard
- [ ] Çağrı kişileri, destek talepleri, etkileşimler, arama kampanyaları, raporlar
- [ ] Çağrı entegrasyonu: SignalR `/hubs/callcenter` üzerinden gelen aktif çağrı → CRM ekranında pop-up/notification

Codex 2026-06-03: CRM CallCenter scope negatif testinde mevcut `codexkokobuyer` kullanicisinin sadece Salon CRM entitlement'i oldugu dogrulandi. `/Home/CallCenter` dogru sekilde `/Home/NoAccess?scope=callcenter` ekranina dustu ve console temizdi. Ancak direkt URL probe'da menu gizli olmasina ragmen `/Contacts`, `/Tickets`, `/Deals`, `/CrmTasks`, `/Surveys`, `/Calls`, `/Activities`, `/Tasks`, `/Campaigns`, `/Integrations`, `/Integrations/Webhooks` gibi Core/CallCenter ekranlari acilabiliyordu. Duzeltme yapildi: CRM controller'larina Core/CallCenter scope guard'lari, CRM proxy'ye de `crm/*`, `crm/salon/*`, `integrations/*` icin scope gate eklendi. `dotnet build src/CallCenter.Crm/CallCenter.Crm.csproj -o .codex-build/crm-scope-guards` PASS. CRM restart sonrasi direct route/proxy retest gerekiyor.

Codex 2026-06-03 retest: CRM scope guard PASS. Restart sonrasi `codexkokobuyer` ile login edildi. Salon-only kullanicida `/Home/Core`, `/Home/CallCenter`, `/Contacts`, `/Tickets`, `/Deals`, `/CrmTasks`, `/Surveys`, `/Calls`, `/Activities`, `/Tasks`, `/Campaigns`, `/Reports`, `/Integrations`, `/Integrations/Webhooks` direkt URL'leri `/Home/NoAccess?scope=core|callcenter` ekranina dustu. Beklenen ortak/satin alma alanlari calismaya devam etti: `/Modules` acildi, `/SalonCrm/Loyalty` acildi. Proxy retest PASS: `/proxy/crm/contacts`, `/proxy/crm/tickets`, `/proxy/crm/dashboard`, `/proxy/integrations/platforms` NoAccess'e dustu; `/proxy/crm/salon/loyalty/config` JSON dondu; `/proxy/crm/modules` JSON dondu.

### 4.4. CRM Multi-Scope
Hem Core + SalonVertical + CallCenterVertical satın alan kullanıcı:
- [ ] Sidebar 3 grup birden açılır
- [ ] Dashboard hangi scope'a düşer? (varsayılan + manuel geçiş)
- [ ] Cross-app navigation switcher (sol üst) — Salon ↔ CRM ↔ CallCenter geçişi

Codex 2026-06-03: Multi-scope testi mevcut hesapla kosulamadi; hesap Salon-only oldugu icin Core/CallCenter gruplari sidebar'da gorunmemesi beklenen davranis. Multi-scope PASS icin Core + Salon + CallCenter CRM paketleri aktif ayri bir kullanici veya bu kullaniciya ek paket aktivasyonu gerekiyor.

### 4.5. CRM Auth ve Hesap
- [x] Login (CRM domain)
- [x] Şifre sıfırlama (`/Account/ForgotPassword`)
- [x] Email doğrulama (`/Account/VerifyEmail`) — AUTH-6 commit
- [x] Logout
- [ ] Concurrent session (aynı kullanıcı 2 tarayıcı)

Codex 2026-06-03: CRM Auth PASS/PARTIAL. Restart sonrasi session dustugu icin login ekranina gelindi; test aracinin hizli typing yolu clipboard kisitina takildi, tek tek keypress ile `codexkokobuyer` girisi yapildi. Yanlis sifre `Kullanıcı adı veya şifre hatalı.` mesajini verdi; dogru sifre `/Home/Salon` dashboard'a gitti. `/Account/ForgotPassword` sayfasi acildi; `/Account/VerifyEmail` token olmadan `Doğrulama bağlantısı geçersiz.` mesajini verdi; `/Account/Logout` login ekranina dondu; console error yok. UI text bug bulundu: login ve forgot ekranlarinda `Şifremi unuttüm/Unuttüm` yaziyordu. `Login.cshtml`, `ForgotPassword.cshtml` ve forgot success mesajinda `Şifremi unuttum`, `e-posta` metinleri duzeltildi. `dotnet build src/CallCenter.Crm/CallCenter.Crm.csproj -o .codex-build/crm-auth-text-scope` PASS. View metin retest icin CRM restart gerekiyor. Concurrent session testi tek in-app tab nedeniyle kosulmadi.

Codex 2026-06-03 retest: CRM Auth text/login/logout PASS. `/Account/Logout` tiklaninca `/Account/Login` acildi; `Şifremi unuttum`, `Kullanıcı Adı`, `Şifre`, `Giriş Yap` metinleri dogru, console error yok. Yanlis sifre denemesi `Kullanıcı adı veya şifre hatalı.` mesajini verdi ve login ekraninda kaldi. Dogru sifreyle `codexkokobuyer` `/Home/Salon` dashboard'a gitti. `/Account/ForgotPassword` sayfasinda `Şifremi Unuttum`, `Sıfırlama bağlantısı e-posta ile gönderilecek`, `Sıfırlama Bağlantısı Gönder`, `Giriş sayfasına dön` metinleri dogru; `/Account/VerifyEmail` tokensiz `Doğrulama başarısız / Doğrulama bağlantısı geçersiz.` gosterdi. Console error yok. Concurrent session testi hala tek in-app tab nedeniyle kosulmadi.

### 4.6. CRM /Payments (Unified Billing Checkout)
Codex 2026-06-03 retest: CRM `/Payments` PASS. API checkout-session callback base `returnApp=crm` icin CRM host'una alindi ve CRM'e public `POST /api/payments/iyzico-callback` proxy eklendi. Restart sonrasi liste PASS: `Salon platform 06/2026`, toplam `20,400.00 TRY` gorundu. Modal PASS. Checkout PASS: Iyzico inline form render oldu, callback HTML icinde `http://localhost:5176/api/payments/iyzico-callback` goruldu. Test kart `5528790000000008`, SKT `12/30`, CVC `123`, ad `TEST USER` ile odeme tamamlandi; URL `http://localhost:5176/Payments?iyzicoToken=795c7d9f-e9ea-42cf-a668-27fcb5bb2881&paid=true`, modal `Odeme basarili / Tahakkuk odemeniz basarili. Kalemler kapatildi.`, liste `Bekleyen odeme bulunmuyor`, console error yok. Not: browser screenshot dosyaya yazma izni yine `EPERM`; gorsel dogrulama arac icinde alindi.

Codex 2026-06-03: CRM `/Payments` PARTIAL/FAIL. Liste PASS: acik tahakkuk `Salon platform 06/2026`, toplam `20,400.00 TRY` gorundu. Modal PASS: `Ödeme Onayı`, kalem ve toplam dogru. Checkout render PASS: `Onaylıyorum, ödemeye geç` sonrasi iyzico sandbox formu inline render oldu (`Kartla Ödeme`, `20.400,00 TL ÖDE`), container `766x560`, console error/CSP violation yok. Iframe yok; form inline geldi. Kart doldurma PASS: iyzico resmi test kart listesine gore basarili kartlar `5528790000000008` ve temiz session'da `5526080000000006`, SKT `12/30`, CVC `123`, ad `TEST USER` ile denendi. Odeme tamamlama FAIL/PENDING: temiz session'da buton tiklama oncesi enabled idi, tiklama kabul edildi; sonra `20.400,00 TL ÖDE` butonu disabled kaldi, 25+ sn beklemede callback/result modal/URL degisimi olmadi, tahakkuk kapanmadi, console error yok. Gorsel dosya yazimi `.codex-run/screenshots` icin tarayici oturumunda `EPERM` verdi; screenshot kaydedilemedi.

- [x] `/Payments` açık tahakkukları listeler
- [x] "Öde" modal açılır, kalemler ve toplam görünür
- [x] "Onaylıyorum, ödemeye geç" → Iyzico hosted page açılır (`paymentPageUrl` veya iframe)
- [x] Test kartla başarılı ödeme → callback → result modal → tahakkuklar kapanır
- [ ] Test kartla başarısız ödeme → result modal → tahakkuklar açık kalır
- [x] **CSP**: `script-src 'self' ... https://*.iyzipay.com https://*.iyzico.com` (commit `c92dbaa`)
- [x] Tarayıcı Console **CSP violation YOK**

---

## 5. Management — Modül Bazlı E2E (P0)

### 5.1. Müşteri Yönetimi
- [x] `/Customers` liste, filtre, arama, pagination
- [x] `/Customers/Detail/{id}`: müşteri özet, modülleri, kullanıcıları, ödemeleri, tahakkukları
- [x] **Toplam Aylık** hesabı doğru (PRICING.9 fix sonrası grup/paket fiyatlandırma)
- [x] Müşteri ekleme/düzenleme/pasif
- [x] Modül atama/çıkarma
- [ ] Manual abonelik aktivasyon

Codex 2026-06-03: Management `/Customers` 5.1 PARTIAL. Liste PASS: tablo yüklendi, arama `koko` ile Enter sonrası 1 sonuca düştü; pagination tek sayfa veride smoke edildi. Müşteri ekleme/düzenleme/pasif PASS: `Codex Mgmt Smoke 341909` kaydı CRM + Salon Yönetimi ürünleriyle oluşturuldu; ilk bug olarak create formu KO `value` binding ve `contactPhone/contactEmail` payload yüzünden telefon/e-posta yazmıyordu. `Index.cshtml`, `Customers.js`, `Detail.cshtml`, `CustomerDetail.js` düzeltildi; restart sonrası detaydan telefon/e-posta kaydedildi, listede göründü ve `Sil` onayıyla kayıt `Pasif` oldu. Modül atama/çıkarma PASS: müşteri detayında Hizmetler sekmesinde `Raporlama` modülü açıldı (`Raporlama ve Analiz 1/2 aktif 1.500 TL / ay`), sonra kapatıldı (`0/2 aktif`), console error yok. Kalan FAIL/PENDING: müşteri genel özette aylık toplam 3.400 TL görünürken Hizmetler sekmesinde yeni kayıt için 0 TL ve modül açılınca grup toplamı 1.500 TL göründü; Toplam Aylık hesabı tutarsız. Ödemeler tabında `Salon platform` satırı alanları boş, aynı anda `Faturalama dönemi yok` boş durumu görünüyor. Manual abonelik aktivasyon henüz koşulmadı. UI copy borcu: Management sidebar/modal metinlerinde `Musteriler`, `Iptal`, `Musteri silindi`, `istediginize` gibi Türkçe karakteri eksik metinler kaldı.

Codex 2026-06-03 devam: 5.1 FIX PENDING RESTART. Management admin localde `codexkokobuyer` test şifresiyle eşitlendi ve browser'da admin login PASS. `/Customers/Detail/1` detayında `koko güzellik merkezi` tekrar açıldı. Canlı bug yeniden doğrulandı: Ödemeler tabında `Salon platform` satırı gelirken dönem/tutar/status hücreleri boş kalıyor ve `Faturalama dönemi yok.` aynı anda görünüyor; Hizmetler tabında `Toplam Aylık: 1.500 TL`, API/müşteri özetinde `salonSubscriptionDisplayMonthly=3.400 TL`. Düzeltme yapıldı: billing foreach alanları `$data`/helper üzerinden güvenli okuyor (`billingPeriodLabel`, `formatMoney`, `billingTotalAmount`), Hizmetler `Toplam Aylık` varsa `salonSubscriptionDisplayMonthly` değerini kullanıyor, modül değişikliklerinden sonra müşteri özeti de yeniden yükleniyor. `node --check src/CallCenter.Management/wwwroot/js/CustomerDetail.js` PASS; `dotnet build src/CallCenter.Management/CallCenter.Management.csproj -o .codex-build/management-customer-detail-fix -p:UseSharedCompilation=false` PASS. Management restart sonrası Ödemeler/Hizmetler retest koşulacak.

Codex 2026-06-03 retest: 5.1 PASS. Management restart sonrası `/Customers/Detail/1` yeniden yüklendi. Genel sekmede `Salon abonelik ozeti` içinde `3.400 TL / ay` göründü. Ödemeler tabı PASS: `Salon platform | 6/2026 | 20400.00 TL | 0.00 TL | 20400.00 TL | Ödenmiş`; `Faturalama donemi yok.` elementi DOM'da kalsa da görünür değil. Hizmetler tabı PASS: `Toplam Aylık: 3.400 TL`. Browser console error yok. Manual abonelik aktivasyon maddesi ayrı açık kalıyor.

### 5.2. Modül ve Paket Yönetimi
- [x] `/Modules` liste (CallCenter portal modülleri)
- [x] `/ServiceManagement` veya benzer: Salon ve CRM modülleri
- [x] Modül fiyatları (`PricingPeriods`): geçmiş ve gelecek dönem fiyatları
- [x] Modül grupları/paketler tanımı
- [x] Müşteri talepleri (`SlnModuleRequest`)
- [x] Modül envanteri (satın alınma sayısı, gelir)

Codex 2026-06-03: Management 5.2 PARTIAL. Sidebar üzerinden gerçek tıklama ile `/Modules/CallCenter`, `/Modules/Salon`, `/Modules/Crm`, `/Modules/PricingPeriods`, `/Modules/Requests`, `/Modules/Inventory`, `/Modules/Subscriptions`, `/Modules/Roles` smoke edildi; console error yok. Önemli uyumsuzluk: `/Modules/CallCenter`, `/Modules/Salon`, `/Modules/Crm` modül kataloğu değil ödeme/tahakkuk takip sayfaları (`CC/Salon/CRM Odeme Takibi`); bu nedenle ilk `/Modules` liste beklentisi açık bırakıldı. `/ServiceManagement` controller'ı `/Modules/PricingPeriods`'a yönlendiriyor. `PricingPeriods` PASS/PARTIAL: sayfa açıldı, kayıt yok, `Yeni Donem` modalı açılıp kapandı; modal metinlerinde `Donem/Olustur/duzenleyip` gibi Türkçe karakter eksikleri var. `Requests` PASS: bekleyen talep yok empty-state ve geçmiş talepler yükle butonu render oldu. `Inventory` PASS: katalog sayfası 54 satır render etti, ilk satırlar Salon modülleri 201-210. `Subscriptions` PASS: 4 plan listelendi (`1/3/6/12 Aylik Abonelik`) ve `Yeni Plan` modalı açıldı. `Roles` smoke PASS: 32 sayfalı rol matrisi render etti, veri değiştirilmedi.

Codex 2026-06-03 devam: 5.2 FIX PENDING RESTART. Canlı `/Modules` probe'u 404 (`ERR_HTTP_RESPONSE_CODE_FAILURE`) verdi; mevcut katalog/liste ekranı `/Modules/Inventory`. `ModulesController.Index()` eklendi ve `/Modules` kök route'u `Inventory` action'ına yönlendiriliyor. `dotnet build src/CallCenter.Management/CallCenter.Management.csproj -o .codex-build/management-modules-index -p:UseSharedCompilation=false` PASS. Management restart sonrası `/Modules -> /Modules/Inventory` retest koşulacak.

Codex 2026-06-03 retest: 5.2 PASS. Management restart sonrası `/Modules?cacheBust=modulesRootRetestClean178` açıldı ve `/Modules/Inventory` sayfasına yönlendi. `Hizmet Envanteri` başlığı göründü, tablo 54 satır render etti; ilk satırlar `201 SlnDashboard`, `202 SlnClients`, `203 SlnAppointments`. Browser console error yok.

### 5.3. Kullanıcı/Personel/Organizasyon
- [x] `/Users` platform kullanıcıları
- [x] `/Personnel` çalışanlar
- [x] `/Organizations` firma yapısı

Codex 2026-06-03: Management 5.3 PASS/PARTIAL. Müşteri Yönetimi grubu sidebar'dan açılarak `/Users`, `/Personnel`, `/Organizations` gerçek tıklamayla test edildi; console error yok. `/Users` PASS: 18 kullanıcı listelendi, `Yeni Kullanici` ve rol filtresi render oldu. `/Personnel` PASS: firma seçmeden boş uyarı, `koko güzellik merkezi` seçilince 6 personel satırı yüklendi. `/Organizations` PASS/PARTIAL: firma seçmeden boş uyarı, `koko güzellik merkezi` seçilince sayfa düzgün kaldı ama kayıtlı organizasyon birimi yok (`Birim bulunamadi`). UI copy borcu: bu üç sayfada `Kullanicilar`, `Yeni Kullanici`, `Firma Secin`, `Lutfen` gibi Türkçe karakteri eksik metinler kaldı.

### 5.4. Ödeme Yapılandırma (`/PaymentConfig`)
- [x] Provider listesi (Iyzico/PayTR/Param)
- [x] **CreateAsync**: bilgileri girip kaydet → DB'de EncryptedCredentials dolu olmalı (CreateAsync boş key geçirmemeli; bilinen bug: validation eksik — issue açın)
- [x] **UpdateAsync**: edit'te boş bırakırsan eski korunur (BUG2.18 fix)
- [x] **TestConnectionAsync**: aktif config'le, gerçek Iyzico API çağrısı
- [x] Banka bilgisi (BankName, IBAN, AccountHolder, Description)
- [ ] Aktif/pasif toggle

Codex 2026-06-03: Management `/PaymentConfig` PARTIAL. Sayfa gerçek tıklamayla açıldı; aktif provider `Iyzico Sandbox`, banka bilgileri dolu (`Test Bankasi`, hesap sahibi, IBAN, açıklama) ve son ödeme işlemleri listesi render oldu. `Baglanti Testi` ikonuna tıklandı; satır `Basarisiz` durumundan `Basarili` durumuna geçti (`Son test: 03.06.2026 02:13:36`), console error yok. CreateAsync, UpdateAsync boş credential koruma ve aktif/pasif toggle veri değiştirdiği için koşulmadı. UI copy borcu: `Odeme`, `Saglayici`, `Basarili`, `Baglanti Testi` gibi metinler Türkçe karakterli değil.

Codex 2026-06-03 retest: 5.4 UpdateAsync PASS. Aktif `Iyzico Sandbox` satırında `Düzenle` açıldı; Iyzico api/secret alanları boş bırakılıp `Kaydet` tıklandı. Modal kapandı, toast `Kaydedildi.` geldi, console error yok. Ardından `Bağlantı Testi` tekrar çalıştırıldı; satır `Basarili`, `Son test: 03.06.2026 09:39:18`. DB helper boolean kontrolü: `CredentialNull=false`, `BankNull=false`, `LastTestSuccess=true`, `IsActive=true`, `IsSandbox=true`. CreateAsync ve aktif/pasif toggle hâlâ açık.

Codex 2026-06-03 devam: 5.4 CreateAsync boş credential bug'i PASS. Canlı `/PaymentConfig` sayfasında boş `Yeni Saglayici` Iyzico save'i mevcut sandbox kaydı nedeniyle duplicate guard'a takıldı ve satır yazmadı. Kod incelemede asıl bug doğrulandı: `PaymentConfigFactory.CreateAsync` boş credential setini doğrulamadan `EncryptCredentialsFromDto` ile boş JSON'u encrypt edip yeni provider kaydı açabiliyordu; update'te de tek credential alanı girilirse eksik set encrypt edilebilirdi. Düzeltme: create için provider'ın tüm zorunlu credential alanları zorunlu, update için credential alanları tamamen boşsa eski değer korunur ama herhangi biri girildiyse setin tamamı istenir. `HasAnyCredential` PayTR salt ve Param password/guid alanlarını da kapsıyor. Doğrulama: `dotnet test tests\CallCenter.Tests\CallCenter.Tests.csproj --no-restore --filter "FullyQualifiedName~PaymentConfigFactoryTests" -o .codex-build\tests-payment-config-factory -p:UseSharedCompilation=false -m:1` PASS 4/4; `dotnet build src\CallCenter.Api\CallCenter.Api.csproj -o .codex-build\api-payment-config-validation -p:UseSharedCompilation=false -m:1` PASS; full suite `tests-all-after-payment-config-validation` 352/352 PASS. Canlı restart sonrası retest PASS: `Yeni Saglayici` modalında `PayTR`, `Sandbox`, `Pasif` seçilip credential alanları boş bırakıldı; save sonrası modal açık kaldı, `PayTR icin credential alanlari eksik: PayTrMerchantId, PayTrMerchantKey, PayTrMerchantSalt.` mesajı geldi, provider tablosunda yeni PayTR/Param satırı oluşmadı, mevcut `Iyzico Sandbox Aktif Basarili` satırı korundu, console error yok.

Codex 2026-06-03 devam: 5.4 PaymentConfig UI copy FIX/PENDING MANAGEMENT RESTART. Canlı sayfada `Odeme/Saglayici/Basarili/Baglanti Testi/Kayit/Iptal` gibi eski ASCII metinler görüldü. `PaymentConfig` view/JS görünür metinleri Türkçe karakterli hale getirildi (`Ödeme Sağlayıcıları`, `Yeni Sağlayıcı Ekle`, `Başarılı/Başarısız`, `Bağlantı Testi`, `Son Ödeme İşlemleri`, `Mutabakat Özeti`, `Ödeme Zaman Çizelgesi` vb.) ve credential/banka inputları `textInput` pattern'ine çekildi. Doğrulama: PaymentConfig view/js eski metin taraması temiz, `node --check src\CallCenter.Management\wwwroot\js\PaymentConfig.js` PASS, `dotnet build src\CallCenter.Management\CallCenter.Management.csproj -o .codex-build\management-paymentconfig-text-fix -p:UseSharedCompilation=false -m:1` PASS. Management restart sonrası canlı UI retest yapılacak.

Codex 2026-06-03 devam: 5.4 PaymentConfig API message copy FIX/PENDING API RESTART. Canlı validation toast'u API'den `PayTR icin credential alanlari eksik...` olarak geliyordu. `PaymentConfigFactory` ve `PaymentConfigController` kullanıcıya dönen mesajları Türkçe karakterli hale getirildi (`PayTR için credential alanları eksik`, `Yapılandırma bulunamadı`, `Ödeme yapılandırması oluşturuldu/güncellendi/aktif edildi` vb.). Doğrulama: görünür mesaj taraması temiz (sadece yorum kaldı), `dotnet test ... --filter "FullyQualifiedName~PaymentConfigFactoryTests"` PASS 4/4, `dotnet build src\CallCenter.Api\CallCenter.Api.csproj -o .codex-build\api-payment-config-message-fix -p:UseSharedCompilation=false -m:1` PASS. API + Management restart sonrası canlı toast/metin retest yapılacak.

Codex 2026-06-03 devam: Management sidebar copy FIX/PENDING MANAGEMENT RESTART. Canlı Management sidebar'da `Musteri Yonetimi`, `Kullanicilar`, `Odeme Takibi`, `Rol Yonetimi`, `Dil Yonetimi`, `Denetim Kayitlari`, `Odeme Ayarlari`, `Email Taslaklari`, `Aydinlatma`, `Basvurular`, `Ihlaller`, `Aktarimlar`, `KVKK Ayarlari`, `Gelismis`, `Cikis` gibi eski ASCII metinler görünüyordu. `_Layout.cshtml` görünür metinleri Türkçe karakterli hale getirildi. Doğrulama: layout eski metin taraması temiz, `dotnet build src\CallCenter.Management\CallCenter.Management.csproj -o .codex-build\management-layout-text-fix -p:UseSharedCompilation=false -m:1` PASS. Management restart sonrası canlı sidebar retest yapılacak.

Codex 2026-06-03 retest/devam: 5.4 PaymentConfig validation PASS + ek metin fix PENDING API + MANAGEMENT RESTART. Restart sonrası `/PaymentConfig` yeni ana metinleri aldı; `Yeni Sağlayıcı Ekle` tıklanarak modal açıldı, sağlayıcı `PayTR`, `Sandbox`, `Pasif` seçildi ve credential alanları boş bırakılarak `Kaydet` tıklandı. Beklenen davranış PASS: modal açık kaldı, toast `PayTR için credential alanları eksik: PayTrMerchantId, PayTrMerchantKey, PayTrMerchantSalt.`, PayTR satırı oluşmadı, mevcut `Iyzico Sandbox Aktif Başarılı` satırı korundu, console error yok. Aynı canlı retestte kalan metin borçları bulundu ve kaynakta düzeltildi: `PaymentConfigController` title `Ödeme Ayarları`, sidebar grup başlıklarında CSS `text-transform: uppercase` kaldırıldı (Türkçe uppercase `I/İ` bozulmasın), ödeme geçmişi status/error display `statusId` ve legacy text normalizer üzerinden Türkçe karakterli hale getirildi, `PaymentStatuses` ve yeni payment cancellation mesajları Türkçe karakterli yapıldı. Doğrulama: `node --check PaymentConfig.js` PASS, `dotnet build src\CallCenter.Api\CallCenter.Api.csproj -o .codex-build\api-payment-history-copy-fix -p:UseSharedCompilation=false -m:1` PASS, `dotnet build src\CallCenter.Management\CallCenter.Management.csproj --no-dependencies -o .codex-build\management-paymentconfig-live-copy-fix-nodeps -p:UseSharedCompilation=false -m:1` PASS. Not: tam Management build ilk denemede çalışan VS/debug `CallCenter.Shared obj` kilidine takıldı; `--no-dependencies` Management değişikliklerini derledi. API + Management restart sonrası başlık/sidebar/ödeme geçmişi metin retest tekrar koşulacak.

### 5.5. Sub-Merchant (PS.4–PS.7)
- [x] `/SubMerchants` salon onboarding kayıtları
- [ ] Pazaryeri split testi: ödeme alımında basketItem.subMerchantKey ve subMerchantPrice eklenmesi
- [x] Hak ediş raporu (`/BillingReport` — PS.10 + PS.13)

Codex 2026-06-03: Management 5.5 PARTIAL. `/SubMerchants` gerçek tıklamayla açıldı; 2 salon listelendi (`Codex Yeni Salon...`, `koko güzellik merkezi`), ikisi de `Baslamadi`, default komisyon %5, console error yok. `/BillingReport` açıldı; 5 tahakkuk satırı render etti, 3 tahakkuk ve 2 ödenmiş kayıt göründü (`koko güzellik merkezi` 20.400 TL ödenmiş dahil), console error yok. Pazaryeri split akışı bu turda yeni ödeme üretmeyi gerektirdiği için koşulmadı. UI copy borcu: `Baslamadi`, `Iletisim`, `Tum`, `Odenmis`, `kayit` metinleri Türkçe karakterli değil.

Codex 2026-06-03 devam: 5.5 UI copy FIX/PENDING API + MANAGEMENT RESTART. `/SubMerchants` görünür metinleri `Pazaryeri Üye İşyerleri`, `Başlamadı`, `Onaylandı`, `İletişim`, `Şahıs`, `Şirket`, `Ltd/A.Ş.`, `varsayılan/özel` olarak düzeltildi; backend `ManagementFactory` onboarding status label'ları da Türkçe karakterli dönecek şekilde güncellendi. `/BillingReport` filtreleri ve tablo metinleri `Yıl`, `Tüm Aylar`, `Şubat/Mayıs/Ağustos/Eylül/Kasım/Aralık`, `Faturalanmış`, `Ödenmiş`, `Gecikmiş`, `seçili`, `Operatör`, `İşlem`, `kayıt` şeklinde düzeltildi; status badge karşılaştırması artık string yerine `statusId` üzerinden yapılıyor. Doğrulama: `node --check SubMerchants.js` PASS, `node --check BillingReport.js` PASS, `dotnet build src\CallCenter.Management\CallCenter.Management.csproj -o .codex-build\management-submerchant-billing-copy -p:UseSharedCompilation=false -m:1` PASS, `dotnet build src\CallCenter.Api\CallCenter.Api.csproj -o .codex-build\api-submerchant-status-copy -p:UseSharedCompilation=false -m:1` PASS. Restart sonrası canlı `/SubMerchants` + `/BillingReport` görsel retest koşulacak.

Codex 2026-06-03 retest: 5.5 `/SubMerchants` + `/BillingReport` PASS. `/SubMerchants` sidebar linkinden açıldı; başlık `Pazaryeri Üye İşyerleri`, açıklama, `Başlamadı/Onaylandı/İletişim/%5 (varsayılan)` metinleri doğru, 2 salon listelendi, eski `Baslamadi/Iletisim/default/Sub-Merchants` metni yok, console error yok. `/BillingReport` açıldı; `Tüm Aylar`, ay adları, `Tüm Durumlar`, `Faturalanmış`, `Ödenmiş`, `Gecikmiş`, `Operatör`, `İşlem`, `Toplam: 5 kayıt` doğru göründü; 5 tahakkuk satırı render oldu, eski `Tum/Odenmis/Operator/kayit` metni yok, console error yok.

### 5.6. Email Template + Storage Config + Translations
- [ ] `/EmailTemplates` CRUD, preview, gönderme
- [x] `/StorageConfig` cloud storage ayarları
- [x] `/Translations`: i18n key/value editör, **Reload Cache** → API + Salon server-side cache yenilenir (SALONI18N.9)

Codex 2026-06-03: Management 5.6 PARTIAL. `/EmailTemplates` ilk retestte FAIL verdi: `Olaylar yuklenemedi`; sebep Management proxy whitelist'inde `platform-email-templates` eksikti. `ProxyPathPolicy.ManagementSegments` içine endpoint eklendi, EmailTemplates form alanları `textInput` pattern'ine çekildi, `dotnet build src\CallCenter.Management\CallCenter.Management.csproj -o .codex-build\management-email-templates-proxy` PASS. Restart sonrası liste PASS: 4 olay geldi (`platform_user_email_verify`, `platform_user_password_reset`, `user_email_verify`, `user_password_reset`), console error yok. `Yeni Olay` modalı ve zorunlu alan validation PASS (`Olay anahtari zorunludur`); bu test ortamında tarayıcı yazma araçları `Browser Use virtual clipboard is not installed` hatasına düştüğü için create/update/delete/preview/gönderme tam CRUD koşulmadı. `/StorageConfig` PASS/PARTIAL: sayfa açıldı, `Yeni Yapilandirma` ve boş state render oldu, console error yok. `/Translations` PASS: 20 satır liste render oldu; `Cache Yenile` tıklandı, toast `Cache yenilendi`, console error yok. UI copy borcu: Email/Storage/Translations sayfalarında `Taslaklari`, `Yapilandirma`, `Yeni Ceviri`, `Olay anahtari`, `yuklenemedi` gibi Türkçe karakter eksikleri var.

Codex 2026-06-03 devam: 5.6 EmailTemplates FIX PENDING RESTART. Karakter-karakter giriş yöntemiyle `codexemailsmoke1780479781948` test olayı oluşturuldu ve açıklaması `Codex smoke updated` olarak güncellendi; create/update PASS, console error yok. Dil taslağı oluşturma sırasında gerçek bug bulundu: TinyMCE CDN yüklenmeyince `window.tinymce` undefined kalıyor; textarea görünür ve dolu olsa bile `saveTemplate` sadece `tinymceEditor.getContent()` kullandığı için `Konu ve HTML icerik zorunludur` uyarısı veriyor. Düzeltme yapıldı: `initEditor` TinyMCE yoksa textarea fallback değerini kuruyor, `saveTemplate` TinyMCE yoksa `#htmlEditor.value` okuyor; ayrıca `EmailTemplates.js` script'ine `asp-append-version="true"` eklendi ki browser eski JS'i cache'ten kullanmasın. `node --check src\CallCenter.Management\wwwroot\js\EmailTemplates.js` PASS; `dotnet build src\CallCenter.Management\CallCenter.Management.csproj -o .codex-build\management-email-template-fallback -p:UseSharedCompilation=false` PASS. Management restart sonrası aynı test kaydıyla dil taslağı kaydetme + silme retest edilecek. Not: ayrı bir "test mail gönder" UI akışı bu sayfada görünmüyor; sadece TinyMCE preview plugin'i var.

Codex 2026-06-03 retest: 5.6 EmailTemplates CRUD PASS/PARTIAL. Restart sonrası script hash'li geldi (`EmailTemplates.js?v=...`), eski cache sorunu bitti. Test olayı `codexemailsmoke1780479781948` üzerinde dil taslağı create PASS: `en` dili eklendi, konu `codex subject`, HTML `<p>codex body</p>`, toast `Taslak olusturuldu`, satırda `en` göründü. Template update PASS: konu/gövde güncellendi, toast `Taslak guncellendi`. Template delete PASS: `Bu Dili Sil` + confirm sonrası satır dilleri tekrar `-`, toast `Dil taslagi silindi`. Event delete PASS: test olayı silindi, liste 4 default olaya döndü, toast `Olay silindi`, console error yok. Preview/gönderme hâlâ PASS değil: bu sayfada ayrı preview veya test mail gönderme butonu/akışı görünmüyor; ürün kararı/geliştirme gerektirir.

Codex 2026-06-03 devam: 5.6 UI copy/form binding FIX/PENDING MANAGEMENT RESTART. `/EmailTemplates`, `/StorageConfig`, `/Translations` görünür metinleri Türkçe karakterli hale getirildi (`E-posta Taslakları`, `Olay Anahtarı`, `Ürün`, `Açıklama`, `Dil Taslakları`, `Yeni Yapılandırma`, `Sağlayıcı`, `Yeni Çeviri`, `Tüm Platformlar`, `Çeviri bulunamadı`, `Önbelleği Yenile` vb.). Bozuk bayraklı dil seçenekleri sade `TR/EN/DE/AR/RU` değerlerine çekildi. Storage/Translations form alanları `textInput` pattern'ine geçirildi; EmailTemplates/Storage/Translations toast ve confirm mesajları Türkçe karakterli hale getirildi. Doğrulama: eski/bozuk metin taraması temiz, `node --check EmailTemplates.js`, `node --check StorageConfig.js`, `node --check Translations.js` PASS, `dotnet build src\CallCenter.Management\CallCenter.Management.csproj -o .codex-build\management-email-storage-translations-copy -p:UseSharedCompilation=false -m:1` PASS. Restart sonrası canlı üç sayfa görsel retest koşulacak.

Codex 2026-06-03 retest/devam: 5.6 EmailTemplates UI copy PASS + default event açıklaması FIX/PENDING API + MANAGEMENT RESTART. Canlı `/EmailTemplates` sayfasında başlık, filtre, tablo kolonları ve aksiyon metinleri Türkçe karakterli göründü, console error yok. Kalan veri metni borcu bulundu: 4 default olay açıklaması DB/seed kaynaklı eski ASCII dönüyordu (`Kullanici email dogrulama maili`, `Salon musteri ... sifre sifirlama maili`). Düzeltme: `PlatformEmailSeedHelper` default açıklamaları Türkçe karakterli hale getirildi ve mevcut legacy açıklamaları idempotent olarak yenileyecek; `PlatformEmailTemplateFactory` hata mesajları Türkçe karakterli yapıldı; Management listesi legacy açıklamaları ekranda normalize edecek şekilde `eventDescription` binding'ine alındı. Doğrulama: `node --check src\CallCenter.Management\wwwroot\js\EmailTemplates.js` PASS, `dotnet build src\CallCenter.Api\CallCenter.Api.csproj -o .codex-build\api-email-template-copy-fix -p:UseSharedCompilation=false -m:1` PASS, `dotnet build src\CallCenter.Management\CallCenter.Management.csproj --no-dependencies -o .codex-build\management-email-template-copy-fix-nodeps -p:UseSharedCompilation=false -m:1` PASS. API + Management restart sonrası `/EmailTemplates`, `/StorageConfig`, `/Translations` canlı retest devam edecek.

Codex 2026-06-03 retest/devam: 5.6 StorageConfig + Translations smoke PASS + title FIX/PENDING MANAGEMENT RESTART. Sidebar linkleriyle `/StorageConfig` ve `/Translations` açıldı, iki sayfada da console error yok. Storage içerik metinleri canlıda Türkçe karakterliydi (`Yeni Yapılandırma`, `Yapılandırma bulunamadı`), kalan title `Depolama Yapilandirmasi` kaynağı controller'da düzeltildi. Translations içerik metinleri canlıda Türkçe karakterliydi (`Yeni Çeviri`, `Tüm Platformlar`, `Önbelleği Yenile`) ve 20 satır render oldu; kalan title `Dil Yonetimi` kaynağı controller'da düzeltildi. Doğrulama: `dotnet build src\CallCenter.Management\CallCenter.Management.csproj --no-dependencies -o .codex-build\management-title-copy-fix-nodeps -p:UseSharedCompilation=false -m:1` PASS. Management restart sonrası başlık retest koşulacak.

Codex 2026-06-03 retest: 5.6 EmailTemplates + StorageConfig + Translations PASS/PARTIAL. Restart sonrası `/Translations` PASS: title `Dil Yönetimi`, `Yeni Çeviri`, `Tüm Platformlar`, `Önbelleği Yenile`, 20 satır, console error yok. `/StorageConfig` PASS: title `Depolama Yapılandırması`, `Yeni Yapılandırma`, `Yapılandırma bulunamadı`, console error yok. `/EmailTemplates` PASS: title `E-posta Taslakları`, tablo kolonları `Olay Anahtarı/Ürün/Açıklama/İşlemler`, 4 default olay açıklaması Türkçe karakterli (`Salon müşterisi`, `e-posta doğrulama`, `Şifre sıfırlama`), eski ASCII açıklamalar görünmüyor, console error yok. EmailTemplates create/update/delete daha önce PASS; ayrı preview/test mail gönderme UI'sı görünmediği için o alt beklenti ürün kararı olarak açık.

Codex 2026-06-03 devam: 5.6 EmailTemplates CRUD retest PASS + preview FIX/PENDING MANAGEMENT RESTART. Gerçek sidebar tıklamasıyla `/EmailTemplates` açıldı; kapalı `Sistem` grubu açılıp sidebar scroll sonrası link hit-test PASS. `Yeni Olay` boş validation PASS (`Olay anahtarı zorunludur`). Karakter-karakter browser girişiyle `codexemail3024710` event'i oluşturuldu; toast `Olay oluşturuldu`, satır listede göründü. `Dil Taslakları` akışında `tr` dili eklendi, konu `codex test konu`, HTML body `codex html icerik` kaydedildi; satırda `tr` badge'i göründü, toast `Taslak oluşturuldu`. Template delete PASS: `Bu Dili Sil` + confirm sonrası satır dilleri `-`, toast `Dil taslağı silindi`. Event delete PASS: test event'i silindi, satır kayboldu, toast `Olay silindi`; console error yok. İki eksik/fix: global Management `confirm-modal.js` içinde `Iptal` -> `İptal` düzeltildi; template modalına sandbox iframe kullanan `Önizle` butonu eklendi. Doğrulama: `node --check EmailTemplates.js` PASS, `node --check confirm-modal.js` PASS, `dotnet build src\CallCenter.Management\CallCenter.Management.csproj --no-dependencies -o .codex-build\management-emailtemplates-preview -p:UseSharedCompilation=false -m:1` PASS. Management restart sonrası `Önizle` butonu ve confirm modal `İptal` metni canlı retest edilecek. Test mail gönderme endpoint/UI hâlâ yok; ürün kapsamı olarak açık.

Codex 2026-06-03 retest: 5.6 EmailTemplates preview PASS. Restart sonrası `EmailTemplates.js` ve `confirm-modal.js` yeni hash ile servis edildi. Default `platform_user_email_verify` olayında `Dil Taslakları` açıldı; `Önizle` butonu görünür, tıklayınca `Taslak Önizleme` modalı açıldı, konu `CorpLynk hesabını doğrula` ve sandbox iframe `srcdoc` dolu geldi. `Bu Dili Sil` tıklanınca global confirm modal `Dil Taslağı Sil / Bu dil taslağı silinecek. Emin misiniz?` açıldı ve cancel butonu artık `İptal`; `İptal` tıklanarak işlem geri alındı, default satır korundu. Browser console error yok. Kalan açık nokta: test mail gönderme için platform email template endpoint/UI yok.

Codex 2026-06-03 devam: Genel metin taramasında `PlatformAuthFactory` public login hata mesajındaki bozuk `Telefon veya ÅŸifre hatalı` metni `Telefon veya şifre hatalı.` olarak düzeltildi; `SlnDtos` içindeki mojibake yorum temizlendi. Doğrulama: `dotnet build src\CallCenter.Api\CallCenter.Api.csproj -o .codex-build\api-platform-auth-copy -p:UseSharedCompilation=false -m:1` PASS.

### 5.7. KVKK + Audit
- [x] `/Kvkk` veri imha talepleri
- [x] `/AuditLogs` filtre, arama, export
- [x] Sensitive alanların maskelendiği doğrula

Codex 2026-06-03: Management 5.7 PARTIAL. `/AuditLogs` gerçek tıklamayla açıldı; 25 satır audit kaydı render oldu, kategori/aksiyon/tarih filtre alanları ve detay ikonları göründü, console error yok. Arama/export bu turda metin girişi/download gerektirdiği için koşulmadı. `/Kvkk` dashboard PASS: toplam onay/bekleyen istek/aktif ihlal/aktif transfer kartları 0, son aktivite empty-state render oldu. `/Kvkk/Requests` PASS/PARTIAL: `Yeni Basvuru`, `Suresi Gecenler`, boş state render oldu. `/Kvkk/Inventory` PASS/PARTIAL: `Yeni Kayit`, boş state render oldu. Sensitive masking derin kontrolü açık kaldı; audit listesinde kullanıcı adı ve localhost IP açık görünüyor, bu tasarım mı maskeleme eksiği mi ayrıca karar gerektiriyor. UI copy borcu: `Denetim Kayitlari`, `Basvurular`, `Aydinlatma`, `Suresi Gecenler`, `Yeni Kayit` gibi metinlerde Türkçe karakter eksiği var.

Codex 2026-06-03 devam: 5.7 AuditLogs FIX PENDING RESTART. Retestte `/AuditLogs` 25 satırla açıldı, console error yok. Kod incelemesinde iki net hata/eksik bulundu: JS tarih filtrelerini `from/to` olarak gönderiyordu ama API `dateFrom/dateTo` bekliyor; bu yüzden tarih filtresi backend'e düşmüyordu. Ayrıca AuditLogs export endpoint/button yoktu. Düzeltme yapıldı: `AuditLogs.js` ortak `buildParams` ile `dateFrom/dateTo` gönderiyor ve `/proxy/auditlogs/export` CSV indiriyor; `Index.cshtml` filtre satırına CSV export butonu eklendi. API tarafında `IAuditLogFactory.ExportCsvAsync`, `AuditLogFactory.ExportCsvAsync`, `GET api/auditlogs/export` eklendi; export aktif filtrelerle aynı sonucu üretir, 10000 satır sınırı var. Detay `OldValues/NewValues` için hassas key maskeleme eklendi (`password`, `token`, `secret`, `credential`, `apikey`, `iban`, `hash` vb. -> `***`). `node --check src\CallCenter.Management\wwwroot\js\AuditLogs.js` PASS; `dotnet build src\CallCenter.Api\CallCenter.Api.csproj -o .codex-build\api-auditlogs-export -p:UseSharedCompilation=false` PASS; `dotnet build src\CallCenter.Management\CallCenter.Management.csproj -o .codex-build\management-auditlogs-export -p:UseSharedCompilation=false` PASS. API + Management restart sonrası filtre/arama/export/maskeleme retest edilecek.

Codex 2026-06-03 retest: 5.7 AuditLogs PASS. Restart sonrası `AuditLogs.js?v=...` hash'li geldi ve CSV export butonu görünür oldu. UI filtre PASS: kategori `Auth`, aksiyon `Login`, arama `codex`, tarih `2026-06-03` ile liste 58 kayda düştü; görünen satırlar `Auth/Login/codexkokobuyer` ve 03.06.2026 tarihliydi, console error yok. Export PASS: aynı filtrelerle API export çağrısı 200 döndü, `Content-Type: text/csv; charset=utf-8`, `Content-Disposition: attachment; filename=audit-logs-...csv`, CSV ilk satırları filtreli `codexkokobuyer` login kayıtlarıydı. Maskeleme PASS: lokal smoke log detail modalında `Password`, `IBAN`, `AccessToken`, `ApiKey`, `ClientSecret` değerleri `***`; normal kontrol alanları `old-ok/new-ok` görünür kaldı; ham secret değer sızıntısı yok. Smoke log satırı test sonrası silindi.

### 5.8. Lisanslama + Bildirimler
- [x] `/Licensing` müşteri lisans durumları
- [x] `/Notifications` platform bildirimleri

Codex 2026-06-03: Management 5.8 PASS/PARTIAL. `/Licensing` ve `/Notifications` açıldı; Gelişmiş menüsü aktifken sidebar linkleriyle gerçek tıklama da doğrulandı, console error yok. `/Licensing` lisans kartlarını 0 değerle ve `Lisanslama sistemi henuz aktif degil. Ileride...` boş/gelecek sürüm mesajıyla gösteriyor. `/Notifications` yeni bildirim formunu ve `Henuz bildirim gonderilmemis. Bildirim sistemi ileri surumde aktif edilecek.` mesajını gösteriyor. Yani sayfalar ayakta ama gerçek lisans/bildirim iş akışı bu sürümde aktif değil. UI copy borcu: `Suresi`, `henuz`, `gonderilmemis`, `ileri surumde` Türkçe karakter eksikleri.

Codex 2026-06-03 devam: 5.8 sidebar click + UI copy FIX/PENDING MANAGEMENT RESTART. Canlı retestte `Lisanslama/Bildirimler` linklerinin DOM'da görünür olmasına rağmen tıklanamadığı bulundu; kök sebep sidebar altındaki sabit kullanıcı barının son linkleri örtmesi. `_Layout` sidebar'a bottom padding/scroll alanı eklendi ve `Rizalar` -> `Rızalar` düzeltildi. `/Licensing` metinleri Türkçe karakterli hale getirildi (`Süresi Dolacak`, `Müşteri Lisansları`, `Başlangıç`, `Bitiş`, `Süresiz`, `henüz aktif değil`, `Seçin`, `İptal`) ve number/date alanları `textInput` pattern'ine çekildi. `/Notifications` metinleri düzeltildi (`Yeni Bildirim Gönder`, `Başlık`, `Tüm Müşteriler`, `Öncelik`, `Uyarı`, `Gönder`, `Geçmiş Bildirimler`, `Henüz bildirim gönderilmemiş`) ve title/message alanları `textInput` oldu. Doğrulama: eski metin taraması temiz, `node --check Licensing.js` PASS, `node --check Notifications.js` PASS, `dotnet build src\CallCenter.Management\CallCenter.Management.csproj --no-dependencies -o .codex-build\management-licensing-notifications-copy-nodeps -p:UseSharedCompilation=false -m:1` PASS. Management restart sonrası `/Licensing` ve `/Notifications` gerçek sidebar tıklama retest yapılacak.

Codex 2026-06-03 retest/devam: 5.8 sidebar click fix ilk hali FAIL -> sticky footer FIX/PENDING MANAGEMENT RESTART. Restart sonrası `/EmailTemplates` üzerinde `Lisanslama` linki hâlâ footer tarafından örtülüyordu (`elementFromPoint` sonucu `System Admin` footer), tıklama URL değiştirmedi. Kök neden padding'in absolute footer overlay'ini kaldırmaması. `_Layout` footer'ı `position:absolute` inline stilden çıkarılıp `.sidebar-footer { position: sticky; bottom: 0; ... }` normal akışına alındı; böylece footer linklerin üstüne binemeyecek. Doğrulama: `dotnet build src\CallCenter.Management\CallCenter.Management.csproj --no-dependencies -o .codex-build\management-sidebar-sticky-footer-nodeps -p:UseSharedCompilation=false -m:1` PASS. Management restart sonrası tekrar gerçek tıklama retest yapılacak.

Codex 2026-06-03 devam: 5.8 sticky footer retest FAIL -> static footer FIX/PENDING MANAGEMENT RESTART. Restart sonrası `.sidebar-footer` canlıya geldi ama `position: sticky` bu layoutta yine viewport dibinde `Lisanslama` linkini örtüyordu (`elementFromPoint` sonucu `System Admin`); tıklama URL değiştirmedi. Final fix: `.sidebar-footer` tamamen normal akışa alındı, `position` kaldırıldı. Sidebar zaten `overflow-y:auto`, bu yüzden footer gerekirse scroll'un en altında duracak ve linklerin üstüne binmeyecek. Doğrulama: `dotnet build src\CallCenter.Management\CallCenter.Management.csproj --no-dependencies -o .codex-build\management-sidebar-static-footer-nodeps -p:UseSharedCompilation=false -m:1` PASS. Management restart sonrası `/Licensing` ve `/Notifications` gerçek tıklama retest yapılacak.

Codex 2026-06-03 retest: 5.8 PASS/PARTIAL. Restart sonrası Management cookie düştü; `admin / OwnerRole2026!` ile login PASS. Doğru kullanıcı akışıyla önce `Gelişmiş` grup başlığı tıklandı, sonra `/Licensing` gerçek sidebar tıklaması PASS: URL `/Licensing`, title `Lisanslama / Paketler`, `Süresi Dolacak`, `Müşteri Lisansları`, `Lisanslama sistemi henüz aktif değil` metinleri doğru, eski ASCII metinler görünmüyor, console error yok. `/Notifications` gerçek sidebar tıklaması PASS: `Yeni Bildirim Gönder`, `Başlık`, `Tüm Müşteriler`, `Öncelik`, `Uyarı`, `Gönder`, `Geçmiş Bildirimler`, `Henüz bildirim gönderilmemiş` metinleri doğru, eski ASCII metinler görünmüyor, console error yok. Boş `Gönder` validation PASS: toast `Başlık ve mesaj zorunlu.`. İş akışları hâlâ gelecek sürüm/placeholder olduğu için gerçek lisans atama ve toplu bildirim gönderimi ürün kapsamı açık.

---

## 6. Cross-App E2E Senaryoları (P0)

### 6.1. Yeni Salon Onboarding
1. [ ] Landing `/register/salon` → form doldur → kayıt
2. [ ] Trial otomatik açıldı, default modüller atandı, default veri seedi
3. [x] Salon `/Account/Login` → JWT → `/Home` dashboard
4. [ ] Onboarding video/wizard akışı (SALONONBOARD.3)
5. [ ] İlk randevu, ilk adisyon, ilk kasa kapama

Codex 2026-06-03: 6.1 PARTIAL. Landing app bu run'da ayakta degildi; `http://localhost:5004` `ERR_CONNECTION_REFUSED` verdi, bu nedenle `/register/salon` kayit/trial/default seed adimlari click-test ile kosulamadi. Mevcut Salon oturumu `/Home` dashboard PASS: toplam musteri 11, bugunun randevusu 1, bugunun cirosu 88 TL, aktif personel 3; console error yok. CRM -> Salon switcher tiklamasi `/Home` dashboard'a dondu.

### 6.2. Yeni Salon + Salon CRM Paketi
- [ ] Salon hesabı var, CRM paketi yok → CRM'e login olunca SalonCrm linkleri görünmez
- [ ] Management'tan SalonCrm paketi atanır
- [x] CRM yeniden login → SalonCrm linkleri görünür
- [x] `/SalonCrm/Loyalty` → 2 tab, Sadakat Programı oluşturma çalışır

Codex 2026-06-03: 6.2 PASS/PARTIAL. Salon app switcher'dan CRM'e tiklandi; CRM login ekranina dusmesi beklenen ayri cookie davranisi olarak not edildi. `codexkokobuyer / OwnerRole2026!` ile login PASS ve `/Home/Salon` acildi. Sidebar'da sadece Salon CRM linkleri gorundu: Sadakat, Uyelikler, Hediye Kartlari, Pazarlama ve SMS, E-posta Kampanyalari, Yorum Yonetimi, Kayip Musteri; console error yok. `/SalonCrm/Loyalty` iki sekmeli geldi ve `Sadakat Programı` sekmesinde `Codex E2E Fon 834125` programi UI'dan kaydedildi. Negatif "CRM paketi yok" ve Management'tan paket atama adimlari bu hesapta zaten paket oldugu icin kosulmadi.

### 6.3. Tahakkuk → Ödeme Akışı
1. [x] Customer aylık tahakkuk açıldı (1700 TL salon platform paketi)
2. [x] Salon kullanıcısı CRM `/Payments` → Öde
3. [x] Iyzico sandbox checkout → test kart → 3DS → callback
4. [x] Tahakkuk **Ödendi** statüsüne geçti
5. [x] Customer Detail'da "Toplam Aylık" doğru
6. [x] PaymentTransactions tablosunda kayıt
7. [ ] Iyzico webhook simülasyonu (settlement event) → audit trail

Codex 2026-06-03: 6.3 current state PASS/PARTIAL. CRM `/Payments` sidebar linkiyle acildi; onceki Iyzico sandbox odemesi sonrasinda ekran `Bekleyen ödeme bulunmuyor.` ve disabled `Ödemeye Geç` gosteriyor, console error yok. Customer Detail `Toplam Aylık`, DB `PaymentTransactions` ve webhook/settlement simulasyonu bu turda yeniden dogrulanmadi.

Codex 2026-06-03 devam: 6.3 PASS/PARTIAL. Management `/Customers/Detail/1` UI doğrulaması PASS: `koko güzellik merkezi` genel özetinde `3.400 TL / ay`, ödemeler tabında `Salon platform | 6/2026 | 20400.00 TL | 0.00 TL | 20400.00 TL | Ödenmiş`; console error yok. DB doğrulaması PASS: `CustomerBillingPeriods` Id=1, BillingKindId=2, 2026/6, Amount=20400, IsPaid=true, StatusId=3, PaidAt dolu. `PaymentTransactions` başarılı kayıt var: Id=23, PaymentTypeId=6, PaymentMethodId=3, StatusId=2, Provider=Iyzico, ProviderTransactionId=`795c7d9f-e9ea-42cf-a668-27fcb5bb2881`, ProviderPaymentId=`34708703`, Amount=20400 TRY, LineCount=1; line `Salon platform 06/2026`, BillingPeriodId=1, Amount=20400 TRY. Not: test planındaki 1700 TL örnek tutar bu müşteri için geçerli değil; canlı senaryoda 3.400 TL/ay x 6 ay = 20.400 TL kapanmış. Webhook/settlement simülasyonu hâlâ açık.

### 6.4. Sadakat Akışı (4 kavram entegrasyonu)
- [ ] Müşteri yeni eklenir
- [x] Salon /Sales: 500 TL hizmet satılır → C (Sadakat Puanı) 500 puan oluşur
- [x] Salon /LoyaltyPackages: 10 öde 12 al paket satışı (A) → kredi bakiyesi
- [x] Salon /Sales: B (multi-session hizmet) satışı → plan oluşur
- [x] CRM /SalonCrm/Loyalty: program oluştur (D — "10 fön → 1 bedava")
- [x] Salon /Sales: 10 fön satıldı → reward oluştu
- [x] 11. fön satışı: reward chip cart'ta → 0 TL kalem
- [x] CRM /SalonCrm/Loyalty: progress tablosunda doğru sayılar

Codex 2026-06-03: 6.4 D reward akisi PASS (kisa senaryo). CRM'de yeni `Codex E2E Fon 834125` programi olusturuldu; ayrica mevcut 1/2 ilerlemeli `Codex Sadakat 20260602223717` programi uzerinden Salon `/Sales`te `Codex POS Smoke 20260602` musterisi secildi, `Saç Kesim` + `hatice güleryüz` + `Sarf yok` ile Nakit 150 TL tahsilat tamamlandi. Sonraki musteri seciminde `Saç Kesim Codex Sadakat 20260602223717` reward chip'i gorundu; chip sepete `Saç Kesim (Sadakat Odulu)` 0 TL kalem olarak eklendi, 0 TL adisyon tamamlandi ve yeniden secimde reward chip kayboldu. Console error yok. UI text borcu bulundu: `Odul/Odulleri/programi` karakterleri duzeltildi (`Sales.js`, `Sales/Index.cshtml`, XML keys); `node --check Sales.js` PASS, `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-sales-loyalty-text-fix` PASS. Restart sonrasi `/js/Sales.js` live check PASS: yeni `Ödül sepete eklendi`, `Sadakat programı ödülü`, `Plan seansı sepete eklendi` metinleri servis edildi, eski ASCII varyantlar yok. A, B ve C puan bakiyesi adimlari bu turda yeniden kosulmadi.

Codex 2026-06-03 devam: 6.4 Fon punch-card full flow PASS. Baslangic CRM `/SalonCrm/Loyalty` program tablosunda `Codex E2E Fon 834125` icin `Codex POS Smoke 20260602` ilerlemesi `2 / 10`, kazanilan `0`, kullanilabilir `0` idi. Salon sidebar tiklamasiyla `/Sales` acildi; ayni musteri secildi, normal `Fon` hizmet karti sepete 100 TL olarak eklendi (aktif seans plan chip'i kullanilmadi), her tur `Sarf yok` + Nakit + personel guard `Devam Et` ile 8 adet Fon tahsilati tamamlandi; tum turlarda `Odeme alindi`, `Sepet bos`, console error yok. CRM final dogrulama: `Codex E2E Fon 834125` satiri `10 / 10`, kazanilan `1`, kullanilabilir `1`. Salon `/Sales` tekrar secimde `Kullanilabilir Sadakat Odulleri` altinda `Fon Codex E2E Fon 834125` chip'i gorundu; chip sepete `Fon (Sadakat Odulu)` 0 TL kalem olarak eklendi, 0 TL adisyon tamamlandi. Son secimde reward chip kayboldu. CRM final: toplam kullanilabilir odul `0`, musteri satiri `10 / 10`, kazanilan `1`, kullanilabilir `0`. Yeni musteri adimi bu turda kosulmadi; mevcut test musterisi uzerinden tamamlandi.

### 6.5. Çok Şubeli Salon
- [x] Salon Owner 2 şube tanımlar
- [x] Şube Müdürü A şubesi için tanımlanır (BranchId=5)
- [x] Şube Müdürü login → sadece A şubesi randevu/müşteri/cash görür
- [x] Sahip → tüm şubeler veya seçici ile geçiş
- [x] CRM Salon vertical owner → branch selector (CRMPROD.9)

Codex 2026-06-03: 6.5 PASS/PARTIAL. Owner `codexkokobuyer` ile `/Branches` sidebar tiklamasi PASS: `koko bostancı` ve `Merkez` aktif subeleri gorundu, owner `/Sales` branch selector'da `Tüm Şubeler/koko bostancı/Merkez` seceneklerini gordu. Yeni personel limiti dolu oldugu icin mevcut `akıra` UI'dan `koko bostancı / Şube Müdürü` yapildi; sifre alaninda tekrar kullanilmis parola guard'i dogru calisti, sifre bos birakilinca personel guncellendi. `akıra / RoleTest2026!` login PASS; branch selector yoktu, sidebar rol bazli daraldi. `/Clients` sadece 2 sube musterisi gosterdi (owner dashboard toplam 11), `/Appointments` branch selector olmadan bos sube takvimi acti, `/Cash` sadece `koko bostancı Kasasi` satirini gosterdi, Merkez/Ana kasa gorunmedi. Console error yok. UI bug bulundu ve duzeltildi: `Şube Normalize` temizleme butonu branch claim'i olan kullanicida gorunuyordu; Appointments/Waitlist'te sadece branch claim'i olmayan owner/manager'a gosterilecek. Kasa API `Sube`/`Kasasi` metni `Şube`/`Kasası` olarak normalize edildi, Cash ekrani `İşlemler` icin yeni key kullaniyor ve `Cash.js` kasa açıldı/açılamadı fallback'leri stale DB key'ine bagli degil. `node --check Cash.js`, `dotnet build src/CallCenter.Api/CallCenter.Api.csproj -o .codex-build/api-branch-cash-fix` ve `dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj -o .codex-build/salon-branch-cash-fix-2` PASS. Restart sonrasi retest PASS: `akıra` role 107 BranchId=5; `/Cash` metinleri `koko bostancı Kasası / Şube / İşlemler` oldu; `/Appointments` ve `/Waitlist` branch selector olmadan acildi, `Şube Normalize` gorunmedi, console error yok. CRM branch selector adimi acik.

Codex 2026-06-03 devam: CRM Salon vertical branch selector PASS. Bug bulundu: `codexkokobuyer` Salon Owner (role 101) olmasina ragmen personel kaydinda `BranchId=1` oldugu icin CRM `CrmSalonController.GetBranchId()` Owner'i de Merkez'e kilitliyordu; bu nedenle `/SalonCrm/Memberships` Yeni Plan modalinda sadece `Tüm Şubeler/Merkez` gorunuyordu. API duzeltildi: `CrmSalonController.GetBranchId()` Owner/Admin icin null scope donuyor, branch-scoped roller eski davranisi koruyor. `dotnet build src/CallCenter.Api/CallCenter.Api.csproj -o .codex-build/api-crm-salon-owner-branch-scope -p:UseSharedCompilation=false` PASS. API restart sonrasi CRM retest PASS: `/SalonCrm/Memberships` Yeni Plan `Şube hedefi` select'i `Tüm Şubeler`, `koko bostancı`, `Merkez`; `/SalonCrm/Campaigns` SMS Kampanyasi modalinda iki branch target select'i de ayni 3 secenegi gosteriyor. Console error yok.

### 6.6. Çağrı Akışı (CallCenter)
- [ ] Inbound call → SignalR notification CRM'e gelir
- [ ] Agent çağrıyı cevaplar → ticket otomatik oluşur
- [ ] Çağrı kaydı (recording) Cloud Storage'a yüklenir
- [ ] Çağrı sonrası supervisor görüntüleyebilir

Codex 2026-06-03: 6.6 SKIPPED/USER DECISION. Kullanici PBX ve Windows App/softphone testlerinin bu turda gecilmesini istedi. Onceki ortam kontrolunde CallCenter.Web ve PBX servisleri ayakta degildi: `http://localhost:5123` (CallCenter.Web) ve `http://localhost:5001` (PBX/PbxService) baglanti reddetti. Bu nedenle inbound call, agent cevaplama, recording ve supervisor goruntuleme akisi bu test turunun kapsami disinda birakildi.

### 6.7. Public Salon → Customer
- [x] `/s/{slug}` profile sayfasına anonim ziyaret
- [ ] Hizmet kombosu seçer (MOBDATA.1)
- [x] Tarih/slot seçer (gerçek `GetAvailableSlotsAsync`)
- [ ] Telefon doğrulama (OTP varsa)
- [x] Randevu oluşur, Salon tarafından görünür
- [ ] No-show deposit varsa Iyzico üzerinden alınır

Codex 2026-06-03: 6.7 PARTIAL. Public landing/discover/profil akışı tıklanarak test edildi; `/discover` harita/listesi ve `/salon/koko-guzellik-merkezi` profil sayfası console errorsiz açıldı. `Online Randevu Al` anonim slot formu yerine public customer login/register gerektiriyor. Yeni public müşteri `Codex Public 524969` / `+905555249690` oluşturuldu ve kayıt sonrası `/salon/koko-guzellik-merkezi/book` wizard açıldı. Hizmet seçimi çalışıyor, fakat `Saç Kesim` ve `Fon` dahil denenen hizmetlerde personel adımı `Bu hizmet için uygun personel bulunamadı` mesajında kalıyor. Public `available-staff` endpointi örnek hizmetlerin tamamında boş döndüğü için tarih/slot, appointment create, no-show deposit ve Salon tarafında görünürlük adımları bu veriyle koşulamadı. Bekleme listesi fallback kaydı PASS ve console error yok.

Codex 2026-06-03 devam: 6.7 root cause bulundu ve FIX PENDING RESTART. Profil slug'ı `koko-guzellik-merkezi` merkez şubeye çözülüyor; merkezde public-visible hizmet personeli yok, uygun personel `koko bostancı` şubesinde. Public booking ekranında şube seçimi yoktu, bu yüzden müşteri çıkmazda kalıyordu. Düzeltme: public branch endpoint'i `slug` döndürüyor, `/book` ekranına `Randevu şubesi` seçici eklendi ve şube değişince ilgili şube booking URL'ine geçiyor. İkinci bug olarak Türkçe karakterli `bostancı` slug'ı proxy guard'da 403'e düşüyordu; `ProxyPathPolicy` Unicode harf/rakam segmentlerine izin verecek şekilde dar güvenli fix aldı, traversal/SSRF yasakları korunuyor. Doğrulama: `node --check PublicBook.js` PASS, `translations-salon.xml` parse PASS, API build PASS, Salon build PASS, `ProxyPathPolicyTests` 22/22 PASS. API + Salon restart sonrası `koko bostancı` seçilip tarih/slot ve appointment create retest edilecek.

Codex 2026-06-03 final retest: 6.7 PASS/PARTIAL. Restart sonrası `/salon/koko-guzellik-merkezi/book` ekranında şube butonları geldi; `koko bostancı` tıklanınca URL `/salon/bostanc%C4%B1/book` oldu ve başlık `koko bostancı - koko güzellik merkezi` gösterdi. `Saç Kesim` seçildi; personel adımında `Fark Etmez`, `akıra balım`, `mualla çöpek`, `sukellamudur` geldi, eski `uygun personel bulunamadı` hatası yok. Gerçek slotlar yüklendi: 14:30-18:30 aralığı. 14:30 seçilip public kullanıcı `Codex Public 524969 / +905555249690` ile `Randevu Al` tamamlandı; public ekranda `Randevunuz Oluşturuldu!` görüldü. Salon `/Appointments` doğrulaması PASS: `3 Haz 14:30`, `Codex Public 524969`, `Saç Kesim`, `akıra balım`, `koko bostancı`, `30 dk`, `Planlanmış Onay Bekliyor`. Combo, OTP ve depozito bu veri/policy ile koşulmadı.

---

## 7. Negative / Edge Cases (P1)

### 7.1. Auth ve Güvenlik
- [x] JWT expired → 401 → login redirect
- [x] Cookie tampered → 401, logout
- [ ] Cross-tenant: A müşterinin verilerini B müşteri JWT'siyle çağırma → 403/404
- [ ] SQL injection denemeleri form alanlarında
- [ ] XSS deneme: arama kutusu, müşteri adı, mesaj alanı
- [x] CSRF: form post'ları, anti-forgery token
- [ ] Brute force: 10 yanlış şifre → kilit
- [x] Proxy SSRF koruma: `/proxy/...` external URL'ye atış engellenir (4fec31f)

Codex 2026-06-03: 7.1 security smoke PASS/PARTIAL. Yetkisiz `/Clients` 302 ile `/Account/Login`'e düştü. Bozuk cookie `CorpLynk.Salon.Auth=not-a-valid-jwt` ve expired JWT payload'i `a.eyJleHAiOjF9.b` ham HTTP'de 302 `/Account/Login` döndürdü; uygulama patlamadı. Auth'lu proxy testinde login cookie sonrası `/proxy/http%3A%2F%2Fevil.com` ve `/proxy/..%2F..%2Fapi%2Fauth%2Flogin` 403 `Proxy path not allowed.`, izinli `/proxy/sln-clients?page=1&pageSize=1` 200 döndü. Kötü `Origin: http://evil.example` ile auth'lu `POST /proxy/sln-clients` 403 `Cross-site proxy request blocked.` verdi. Yanlış şifre POST'u 200 login formu + hata mesajı ile kaldı. Brute-force lockout, SQLi/XSS ve cross-tenant negatifleri bu turda koşulmadı.

Codex 2026-06-03 devam: 7.1 Management `/Customers` XSS/SQLi smoke PASS + stored-XSS FIX/PENDING MANAGEMENT RESTART. `/Customers` gerçek sidebar tıklamasıyla açıldı. Arama alanına `"><img src=x onerror=alert(1)>` girilip Enter basıldı; sonuç 0 satıra düştü, tabloda `script/img[onerror]` elementi oluşmadı, raw HTML body'ye enjekte edilmedi, console error yok. SQLi benzeri `' OR 1=1 --` araması 0 satır döndürdü, console error yok. Kod incelemede gerçek stored-XSS riski bulundu: silme modalı `data-bind="html: deleteMessage"` ile müşteri adını HTML olarak basıyordu. Düzeltme: `deleteMessage` düz metne çevrildi ve view `text: deleteMessage` kullanıyor. Aynı pakette `/Customers` görünür metinleri Türkçe karakterli hale getirildi (`Müşteriler`, `Aylık Faturalama`, `Yeni Müşteri`, `Firma Adı`, `Modüller`, `kayıt`, `Toplu Dönem Oluştur`, ay adları, toast mesajları vb.). Doğrulama: `node --check Customers.js` PASS, `dotnet build src\CallCenter.Management\CallCenter.Management.csproj --no-dependencies -o .codex-build\management-customers-xss-copy-nodeps -p:UseSharedCompilation=false -m:1` PASS. Management restart sonrası `/Customers` metin + silme modalı retest yapılacak.

Codex 2026-06-03 retest: 7.1 Management `/Customers` LIVE STILL STALE. Reload sonrası canlı sayfa hâlâ `title=Musteriler - CorpLynk Management`, placeholder `Firma adi, e-posta veya telefon ara...`, eski `Aylik Faturalama` ve delete modal binding `html: deleteMessage` servis ediyor. Statik `/js/Customers.js` yeni dosyayı döndürüyor ve kaynakta `text: deleteMessage` var; sorun source değil, çalışan Management assembly/view eski. Management normal build + restart sonrası aynı retest tekrar koşulacak.

Codex 2026-06-03 retest: 7.1 Management `/Customers` source fix LIVE PASS. Restart sonrası `/Customers?cacheBust=...` title `Müşteriler - CorpLynk Management`, başlık `Müşteriler`, placeholder `Firma adı, e-posta veya telefon ara...`, `Aylık Faturalama`, `Yeni Müşteri` doğru geldi. Eski `Musteriler`, `Aylik Faturalama`, `Yeni Musteri` metinleri görünmedi. Delete modal binding artık `text: deleteMessage`; stored-XSS riski canlı view'da kapanmış görünüyor. Console error yok.

### 7.2. Data Validation
- [x] Boş/null alan: zorunlu alan boşsa 400
- [x] Çok uzun string (>255 char)
- [x] Geçersiz email/telefon format
- [x] Negative number where positive expected
- [x] Date past/future limits
- [ ] Concurrent edit (aynı entity 2 user)

Codex 2026-06-03: 7.2 validation PASS/PARTIAL. Click testte `/Appointments` yeni randevu boş submit kayıt yapmadan `Tarih ve saat zorunludur` uyarısı verdi; `/Clients` yeni müşteri boş submit kayıt yapmadan `Ad soyad ve telefon zorunludur` uyarısı verdi. Kod incelemede backend validasyon boşlukları bulundu: müşteri create/update telefon/e-posta/beyaz oran ve uzun string kontrollerini API seviyesinde yapmıyordu; hizmet create/update negatif fiyat, 0 süre, 0 seans ve negatif süreleri sessiz geçirebiliyordu; randevu create/update geçmiş/aşırı ileri tarihleri reddetmiyordu. Düzeltildi: `SlnClientFactory`, `SlnServiceFactory`, `SlnAppointmentFactory`, `SlnClientController`, `CrmSalonController`. Regression: `dotnet test tests/CallCenter.Tests/CallCenter.Tests.csproj --no-restore --filter "FullyQualifiedName~SlnClientFactoryTests|FullyQualifiedName~SlnServiceFactoryTests|FullyQualifiedName~SlnAppointmentFactoryTests" -o .codex-build/tests-validation -p:UseSharedCompilation=false -m:1` PASS 48/48; full suite son turda PASS 348/348. `dotnet build src/CallCenter.Api/CallCenter.Api.csproj -o .codex-build/api-validation -p:UseSharedCompilation=false -m:1` PASS. Restart sonrası canlı UI retest PASS: `/Clients` menüden açıldı, `Yeni Müşteri` boş kaydet kayıt oluşturmadan `Ad soyad ve telefon zorunludur` toast'u verdi, console error yok. Concurrent edit ve tüm negatif kombinasyonlar canlı UI'da koşulmadı.

### 7.3. UI Davranışı
- [x] Native `alert/confirm/prompt` yok — sadece `confirmModal` + `toastr`
- [ ] AJAX 401 → global handler login redirect
- [ ] AJAX 403 → sayfanın kendi `.fail` handler'i
- [x] KO foreach: `$data.prop` ile güvenli erişim
- [x] `createAutocomplete` `xxxAutocomplete.query` pattern (with binding değil)
- [x] Razor `@page` directive ile çakışan değişken adı yok

Codex 2026-06-03: 7.3 UI static smoke PASS/PARTIAL. Native dialog taramasında tek gerçek kullanım bulundu: `src/CallCenter.Crm/wwwroot/js/SalonLoyaltyProgram.js` içinde `window.confirm` fallback'i vardı. Aynı blokta `confirmModal` helper yanlış imzayla çağrılıyordu; `confirmModal(title, message, onConfirm, options)` pattern'ine çekildi, native confirm fallback'i kaldırıldı. `node --check src/CallCenter.Crm/wwwroot/js/SalonLoyaltyProgram.js` PASS, `dotnet build src/CallCenter.Crm/CallCenter.Crm.csproj -o .codex-build/crm-loyalty-confirm-fix` PASS. Restart sonrası click-retest PASS: `/SalonCrm/Loyalty` -> `Sadakat Programı` sekmesinde `Codex E2E Fon 834125` sil butonu Bootstrap modal açtı (`Onayla`, `Bu program silinsin mi?`, `İptal/Sil`), native confirm yok, `İptal` sonrası satır yerinde kaldı, console error yok. `createAutocomplete` + `data-bind="with: xxxAutocomplete"` static taramasında Salon'da çok sayıda eski kullanım bulundu (`Appointments`, `Sales`, `Products`, `Recipes`, `Expenses`, `Invoices`, `Memberships`, `LoyaltyPackages`); bu geniş refactor ayrı riskli iş olarak açık bırakıldı.

Codex 2026-06-03 devam: 7.3 autocomplete binding PASS/PARTIAL. Native `alert/confirm/prompt` yeniden tarandı; sadece `confirm-modal.js` yorum satırı yakalandı, gerçek native dialog kullanımı yok. Razor `@page`/`page` çakışması taraması temiz. Eski `with: xxxAutocomplete` context pattern'i dar view değişikliğiyle açık referanslara taşındı: `Sales`, `Appointments`, `Marketing/_MembershipsPane`, `LoyaltyPackages`, `Products`, `Expenses`, `Invoices`, `Recipes`. Tekrar taramada `with: ...Autocomplete` kalmadı. `dotnet build src\CallCenter.Salon\CallCenter.Salon.csproj -o .codex-build\salon-autocomplete-context-fix -p:UseSharedCompilation=false -m:1` PASS. Canlı click smoke: `/Appointments` yeni randevu müşteri autocomplete yeni binding ile açıldı, `yeter` yazınca seçenek verisi geldi ve `ArrowDown` ile dropdown `yeter güleryüz` olarak görünür oldu; console error yok. `/Sales` menüden açıldı, müşteri autocomplete `Codex` ile dropdown açtı ve müşteri listesi geldi; console error yok. `/Products` menüden açıldı, yeni ürün modalında kategori `sac` ve marka `codex` autocomplete dropdown'ları açıldı; console error yok. `/Recipes` menüden açıldı, yeni reçete modalında ürün autocomplete `sac boyası`, hizmet autocomplete `fon` -> `Fon` olarak çalıştı; console error yok. `LoyaltyPackages` menüden açıldı; bu sayfada eski assignment client autocomplete bloğu artık DOM'da yok, sayfa eski `with:` pattern'i içermiyor. Mevcut kullanıcı menüsünde `/Expenses`, `/Invoices` ve Salon `/Marketing` görünür link olmadığı için bu üçü canlı tıklamalı koşulmadı; statik tarama/build temiz.

### 7.4. Translations
- [ ] Yeni eklenen TR key'in EN karşılığı var
- [ ] Eksik key → fallback (anahtar adı veya defaultText)
- [ ] Management translation reload → Salon server-side cache yenilenir

Codex 2026-06-03: 7.4 translation XML smoke PASS/PARTIAL. `translations-salon.xml` parse PASS, 3135 key, eksik TR=0, eksik EN=0. `translations-management.xml` parse PASS, 185 key, eksik TR=0, eksik EN=0. CRM XML'leri parse PASS ama EN borcu var: `translations-crm.xml` 326 key içinde 146 EN eksik; `translations-crm-salon-tr-patch.xml` 199 key içinde 199 EN eksik. Örnek eksikler: `crm.common.status.scheduled`, `crm.salon.common.all_branches`, `crm.salon.loyalty.page_title`, `crm.salon.loyalty.tab.points`, `crm.salon.loyalty.tab.program`. Management reload -> Salon cache yenileme bu turda koşulmadı.

Codex 2026-06-03 retest: 7.4 Management translation reload PASS/PARTIAL. Management sidebar'dan `/Translations` gerçek tıklamayla açıldı; title `Dil Yönetimi`, `Önbelleği Yenile` butonu göründü. Buton tıklanınca toast `Önbellek yenilendi.`, console error yok. Bu UI/API reload akışı geçti; Salon server-side cache'in ayrı process içinde yenilendiğini uçtan uca ölçen ek doğrulama hâlâ açık.

### 7.5. Tarih ve TZ
- [x] UTC çift dönüşüm yok (BUG2.17 fix)
- [x] Türkiye UTC+3 sabit, DST yok
- [x] JS → API tarih: UTC suffix tutarlı

Codex 2026-06-03: 7.5 TZ/date-only PASS/PARTIAL. Randevu akışındaki BUG2.17 slot seçimi korunuyor: API'den `...Z` ile gelen salon local saatleri `new Date()` ile parse edilmiyor, string olarak `Z` temizlenip input'a yazılıyor; save aşamasında API kontratı için tek kez `Z` ekleniyor. Yeni statik taramada date-only inputlarda `toISOString().substring/slice(0,10)` kullanımı bulundu; Türkiye UTC+3'te gece 00:00-02:59 arasında günü bir önceye düşürebileceği için local `YYYY-MM-DD` helper'a çekildi. Düzeltilen dosyalar: `Appointments.js`, `Reports.js`, `Expenses.js`, `Waitlist.js`, `PublicProfile.js`, CRM `Campaigns.js`, Management `PricingPeriods.js`, `Subscriptions.js`. Gerçek timestamp/funnel alanlarındaki `new Date().toISOString()` kullanımları bırakıldı. Doğrulama: kalan `toISOString().substring/slice(0,10)` taraması temiz; `node --check` 8/8 PASS; `dotnet build` Salon/CRM/Management ayrı output ile PASS. Canlı Salon retest PASS: public randevu akışında `Codex Appt Note 591249` için `03.06.2026 16:00` seçildi, `/Appointments` listesinde aynı kayıt `3 Haz 16:00` olarak göründü; public waitlist kaydı `2026-06-03` tarihini `3.6.2026` gösterdi. CRM/Management date-only helper'ları build/statik doğrulandı, ayrıca tarayıcıda yeniden gezilmedi.

---

## 8. Security / CSP / Headers (P0)

### 8.1. Response Headers (her uygulamada)
- [x] `X-Content-Type-Options: nosniff`
- [x] `X-Frame-Options: SAMEORIGIN`
- [x] `Referrer-Policy: strict-origin-when-cross-origin`
- [x] `Permissions-Policy: camera=(), microphone=(), geolocation=(self)`
- [x] `Content-Security-Policy:` script-src include iyzipay/iyzico (commit `c92dbaa`)
- [ ] Strict-Transport-Security (production)

Codex 2026-06-03: 8.1 response headers local smoke PASS/PARTIAL. Salon `/` 200, CRM `/` 302 login redirect, Management `/` 302 login redirect, API `/health` 200 cevaplarında `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy: camera=(), microphone=(), geolocation=(self)`, `Cross-Origin-Opener-Policy: same-origin-allow-popups` ve CSP header'ı görüldü. CSP içinde `https://*.iyzipay.com` ve `https://*.iyzico.com` script/frame/connect/form izinleri var. `Strict-Transport-Security` local HTTP'de beklenmedi; production HTTPS üzerinde ayrıca kontrol edilmeli.

### 8.2. CSRF
- [x] MVC form POST'larında anti-forgery token
- [x] AJAX/proxy POST: same-origin guard

Codex 2026-06-03: 8.2 CSRF proxy smoke PASS/PARTIAL. Auth'lu Salon cookie ile `Origin: http://evil.example` header'lı `POST /proxy/sln-clients` 403 `Cross-site proxy request blocked.` döndü. Aynı turda auth'lu proxy path guard da PASS: dış URL ve traversal path 403, izinli `/proxy/sln-clients?page=1&pageSize=1` 200. MVC form anti-forgery token kapsamı ve tüm AJAX POST token taraması bu turda tam koşulmadı.

Codex 2026-06-03 devam: 8.2 CSRF klasik form kapsamı PASS/PARTIAL. Statik taramada Salon/CRM/Management login, forgot password, reset password ve resend verification formlarında anti-forgery token yoktu; ilgili form POST action'larında `[ValidateAntiForgeryToken]` de yoktu. Düzeltildi: Salon `AccountController`, `PublicSalonController`; CRM `AccountController`; Management `AccountController`; ilgili Account/PublicSalon Razor formlarına `@Html.AntiForgeryToken()` eklendi. Salon subscription-required logout formuna token eklendi ve Salon logout GET/POST ayrıldı; GET linkleri çalışmaya devam ediyor, POST token doğruluyor. Canlı HTTP retest PASS: Salon login/forgot/resend/reset, public forgot/resend/reset, CRM login/forgot/reset ve Management login/forgot/reset GET sayfalarında `__RequestVerificationToken` var; token'sız POST hepsinde 400 döndü. AJAX proxy tarafında token header pattern'i yerine mevcut `ProxyCsrfGuard.IsSafeOrSameOrigin` kullanılıyor; kötü Origin testi daha önce 403 PASS verdi ve `ProxyCsrfGuardTests` PASS 3/3. Public JSON endpointleri için bearer/auth ve origin kapsamı ayrıca tam negatif test edilmedi.

### 8.3. Webhook İmza
- [x] Iyzico webhook signature verify (`IyzicoWebhookSignatureValidator`)
- [x] Geçersiz imza → 401

Codex 2026-06-03: 8.3 webhook signature test PASS. İzole test koşuldu: `dotnet test tests/CallCenter.Tests/CallCenter.Tests.csproj --no-build --filter FullyQualifiedName~IyzicoWebhookSignatureValidatorTests` -> 5/5 PASS. İlk normal `dotnet test` denemesi NuGet restore sonrası VS/API debug lock yüzünden build copy aşamasında kaldı; `--no-build` mevcut çıktı ile başarıyla koştu. Runtime invalid webhook endpoint testi bu turda ayrıca yapılmadı.

Codex 2026-06-03 devam: 8.3 runtime invalid signature PASS + message FIX/PENDING API RESTART. Canlı API'ye `POST http://localhost:5041/api/payments/iyzico-webhook` invalid `X-IYZ-SIGNATURE-V3` ile gönderildi; beklenen 401 döndü ve veri işlenmedi. Mesajda Türkçe karakter borcu görüldü (`Webhook imzasi gecersiz.`). `PaymentController` webhook hata mesajları düzeltildi: `Webhook payload boş veya event type eksik.`, `Aktif Iyzico ödeme yapılandırması bulunamadı.`, `Webhook imzası geçersiz.`. Doğrulama: `dotnet build src\CallCenter.Api\CallCenter.Api.csproj -o .codex-build\api-webhook-message-fix -p:UseSharedCompilation=false -m:1` PASS; `dotnet test tests\CallCenter.Tests\CallCenter.Tests.csproj --no-build --filter FullyQualifiedName~IyzicoWebhookSignatureValidatorTests` PASS 5/5. API restart sonrası invalid signature mesaj retest yapılacak.

Codex 2026-06-03 retest: 8.3 runtime invalid signature behavior PASS, message STILL STALE. Canlı API invalid signature isteğine yine 401 döndü ve veri işlenmedi; fakat response hâlâ `{"message":"Webhook imzasi gecersiz."}`. Kod taramasında bu eski metin source içinde kalmadı; `PaymentController` yeni metni `Webhook imzası geçersiz.` olarak içeriyor. API normal build + restart sonrası mesaj retest tekrar koşulacak.

Codex 2026-06-03 retest: 8.3 runtime invalid signature final PASS. API restart sonrası invalid `X-IYZ-SIGNATURE-V3` ile `POST /api/payments/iyzico-webhook` 401 döndü, veri işlenmedi ve response artık `{"message":"Webhook imzası geçersiz."}`. İzole validator testleri daha önce 5/5 PASS idi.

### 8.4. API Key (Integration)
- [x] `/api/integration/v1/*` `X-Api-Key` middleware kontrolü
- [x] Eksik/geçersiz key → 401

Codex 2026-06-03: 8.4 API key PASS/PARTIAL. Canlı API negatif smoke: `GET /api/integration/v1/health` header olmadan 401 `X-Api-Key header gerekli.`, `X-Api-Key: invalid_key` ile 401 `Gecersiz API key.` döndü. Factory API key testleri koşuldu: `dotnet test ... --no-build --filter "FullyQualifiedName~IntegrationFactoryTests&FullyQualifiedName~ApiKey"` -> 4/4 PASS. Geçerli API key ile pozitif integration endpoint akışı bu turda DB key oluşturmadan koşulmadı.

### 8.5. Encryption at rest
- [x] `PlatformPaymentConfig.EncryptedCredentials` AES-256-CBC
- [ ] SIP password encrypted

Codex 2026-06-03: 8.5 encryption smoke PARTIAL. Kod doğrulaması: `AesEncryptionService` AES-256-CBC kullanıyor ve `Base64(IV + ciphertext)` formatında saklıyor; `PlatformPaymentConfig.EncryptedCredentials`, `EncryptedBankInfo`, SIP password, email/storage/integration credentials aynı servis/pattern üzerinden şifreleniyor. Test: `dotnet test ... --no-build --filter "FullyQualifiedName~IntegrationFactoryTests&FullyQualifiedName~CreateConnection_WithCredentials"` -> 1/1 PASS; kaydedilen credential içinde plain `secret_token_123` bulunmadığını doğruluyor. `PlatformPaymentConfig` ve SIP password için DB üzerinden canlı plaintext kontrolü bu turda koşulmadı.
- [x] Cloud storage tokens encrypted (kod yolu dogrulandi; canli DB plaintext kontrolu kosulmadi)

Codex 2026-06-03: 8.5 ek dogrulama. `CustomerStorageConfig.EncryptedCredentials` entity alani credential JSON icin kullaniliyor; `CloudStorageFactory.CreateConfigAsync` tokenlari `AesEncryptionService.Encrypt(...)` ile sakliyor, `UpdateConfigAsync` mevcut sifreli JSON'u decrypt edip merge sonrasinda tekrar encrypt ediyor. Dedicated storage unit testi ve canli DB plaintext sorgusu yok; bu nedenle kod yolu PASS, veri tabani canli denetimi risk olarak acik.

---

## 9. Performance / Smoke (P2)

- [ ] CRM Contacts 10000+ kayıtla liste pagination süresi (< 2s)
- [ ] Salon Randevu Takvimi aylık görünüm 500+ randevu (< 3s)
- [ ] Reports/Settlement raporu büyük veri (< 5s)
- [ ] SignalR ile 50+ eşzamanlı agent
- [x] Public Salon profil sayfası TTFB (< 1s)
- [x] Mobil responsive: 320px–768px (UX.4)

Codex 2026-06-03: 9.x performance smoke PARTIAL. Gercek yuk testi onkosullari yok: CRM `codexkokobuyer` hesabi Salon vertical scope ile calisiyor, `/Contacts` core scope istedigi icin `/Home/NoAccess?scope=core` ekranina dustu; `/Reports` callcenter/core scope gerektirdigi icin `/Home/NoAccess?scope=callcenter` verdi. Salon `/Appointments` auth sonrasi gercek ekranda acti, mevcut lokal veriyle 2 randevu ve 0 console error, browser elapsed ~1.7s; 500+ randevulu ay verisi olmadigi icin hedef performans kosulmadi. Salon `/Reports` module 211 aktif olmadigi icin `ModuleRequired` ekranina yonlendi; buyuk veri raporu kosulamadi. Public profil HTTP TTFB PASS: `/salon/koko-guzellik-merkezi` 200, TTFB 0.035s; `/book` 200, TTFB 0.093s. Responsive smoke PASS/PARTIAL: public profil 390px ve 768px viewport'ta horizontal overflow yapmadi, console error yok; tam UX gorsel inceleme acik.

---

## 10. Deploy Kabul Kapısı

### 10.1. Pre-deploy Checklist
- [x] Root Dockerfile API'ye ait (`head -3 Dockerfile` doğrula)
- [ ] `git status --short` temiz
- [x] Build + 348 test PASS
- [ ] Browser smoke (bu doc Bölüm 2)
- [ ] Migration eski/yeni uyumlu (varsa)
- [ ] ClaudeManager Notes'tan ilgili deploy notu okundu

Codex 2026-06-03: 10.1 local pre-deploy gate PARTIAL. Kok `Dockerfile` eksikti; `/Dockerfile` `.gitignore` altinda local deploy dosyasi oldugu icin API Dockerfile icerigiyle yeniden olusturuldu ve `Get-Content Dockerfile -TotalCount 5` API multi-stage build basligini dogruladi. `git status --short` temiz degil; beklenen cok sayida aktif degisiklik var. `dotnet test tests/CallCenter.Tests/CallCenter.Tests.csproj --no-build` PASS: 339/339. `dotnet build CallCenter.slnx --no-restore` bu kez kod hatasindan degil calisan VS/debug process lock'larindan FAIL oldu: `CallCenter.Api`, `CallCenter.Salon`, `CallCenter.Crm`, `CallCenter.Management` bin dosyalari Visual Studio ve calisan app processleri tarafindan kilitli. Temiz build icin uygulamalar kapali halde tekrar kosulmali.

Codex 2026-06-03 devam: payment config validation sonrasi full test PASS. `dotnet test tests\CallCenter.Tests\CallCenter.Tests.csproj --no-restore -o .codex-build\tests-all-after-payment-config-validation -p:UseSharedCompilation=false -m:1` 352/352 PASS.

Codex 2026-06-03 devam: PaymentConfig copy/message fix sonrasi full test PASS. `dotnet test tests\CallCenter.Tests\CallCenter.Tests.csproj --no-restore -o .codex-build\tests-all-after-payment-config-copy -p:UseSharedCompilation=false -m:1` 352/352 PASS.

Codex 2026-06-03 devam: Full test suite PASS. Çalışan uygulama process lock'larına takılmamak için ayrı output klasörüyle `dotnet test tests\CallCenter.Tests\CallCenter.Tests.csproj --no-restore -o .codex-build\tests-all-after-notes-csrf -p:UseSharedCompilation=false -m:1` koşuldu; sonuç 348/348 PASS. Tam solution build hâlâ uygulamalar açıkken koşulmadı; processler kapalıyken tekrar denenmeli.

Codex 2026-06-03 devam: Son client detail branch/text fixlerinden sonra full suite tekrar PASS. `dotnet test tests\CallCenter.Tests\CallCenter.Tests.csproj --no-restore -o .codex-build\tests-all-after-client-detail-fixes -p:UseSharedCompilation=false -m:1` -> 348/348 PASS. API + Salon canlı retest için restart bekleniyor.

Codex 2026-06-03 post-commit gate PASS/PARTIAL. Commit `bfbac12` sonrasi full test suite ayri output ile tekrar kosuldu: `dotnet test tests\CallCenter.Tests\CallCenter.Tests.csproj --no-restore -o .codex-build\tests-after-checkpoint -p:UseSharedCompilation=false -m:1` -> 352/352 PASS. Ayrica process lock'a takilmamak icin ayri output klasorleriyle proje buildleri kosuldu ve hepsi 0 error PASS: API (`.codex-build\api-after-checkpoint`), Salon (`.codex-build\salon-after-checkpoint`), CRM (`.codex-build\crm-after-checkpoint`), Management (`.codex-build\management-after-checkpoint`). Browser click-test devami BLOCKED: Chrome extension secili profilde kurulu gorunmuyor ve native host registry kaydi eksik raporlandi; bu nedenle canli tiklamali smoke bu turda kosulamadi.

Codex 2026-06-03 migration design-time PASS/PARTIAL. `dotnet ef migrations list --no-build --project src\CallCenter.Data --startup-project src\CallCenter.Api` PASS ve migration listesi `20260531221820_AddSalonServiceSessionTracking` son kaydina kadar okundu. Standart `dotnet ef migrations list` ve normal `dotnet build src\CallCenter.Api\CallCenter.Api.csproj` bu turda kod hatasindan degil calisan `CallCenter.Api (2792)` ve `Microsoft Visual Studio Insiders (23212)` process lock'undan dolayi bin kopyalama asamasinda fail oldu. Uygulamalar kapaliyken build'li EF listesi tekrar kosulabilir; design-time factory ve migration discovery `--no-build` ile calisiyor.

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

# Call Center Projesi - Geliştirme Desenleri ve Kararlar

## Çalışma Kuralları (KESİN)

1. **COMMIT**: Kullanıcı açıkça "commit et" demeden asla commit yapılmayacak.
2. **RUN/DEBUG**: Asla `dotnet run`, `dotnet watch` veya benzeri çalıştırma komutu kullanılmayacak. Test ve debug Visual Studio'da yapılacak.
3. **DÜRÜSTLÜK**: Eksik varsa eksik, yanlış olma ihtimali varsa ihtimal raporlanacak. Yalan yok.
4. **RİSK RAPORU**: Her tamamlanan adımın altına olası riskler/eksikler `yol_haritasi.xml`'e `<Riskler>` etiketi ile yazılacak.
5. **CONTEXT**: Her oturumda `patterns.md` ve `yol_haritasi.xml` okunacak.

## Veritabanı Kuralları (KESİN)

1. **ID TİPİ**: Tüm entity'lerde primary key `int Id` olacak (auto-increment). Asla `Guid` PK kullanılmayacak.
2. **UID ALANI**: Dışarıya açık entity'lerde (User, Customer, CallRecord vb.) `Guid Uid` alanı olacak. Bu alan URL/link'lerde kullanılacak (güvenlik: int ID dışarıya expose edilmez).
3. **FK İLİŞKİLERİ**: Foreign key'ler `int` tipinde olacak (performans).
4. **DAHİLİ TABLOLAR**: Translation, TranslationKey gibi sadece dahili kullanılan tablolarda Uid gerekmez, sadece `int Id` yeterli.

## Mimari Kararlar

### Neden Blazor Hybrid?
- **Problem**: 3 platform (Web, Windows, Mobil) için 3 ayrı UI yazmak çok fazla iş
- **Çözüm**: Blazor ile UI bir kere yazılır, Blazor Hybrid ile her platformda çalışır
- **Web**: Blazor WebAssembly → tarayıcıda çalışır
- **Windows**: WPF + WebView2 → Blazor UI'ı native pencerede gösterir
- **Mobil**: MAUI + BlazorWebView → Blazor UI'ı mobil uygulamada gösterir

### Neden SignalR?
- Call center gerçek zamanlı olmalı (çağrı bildirimleri, kuyruk güncellemeleri, agent durumları)
- SignalR zaten ASP.NET Core ile gömülü, ekstra altyapı gerekmez
- WebSocket tabanlı, düşük gecikmeli

### Neden Dinamik SIP?
- Müşteriler farklı SIP sağlayıcılar kullanabilir (Asterisk, FreeSWITCH, 3CX, Twilio)
- SIP bilgileri (sunucu, port, kullanıcı, şifre) admin panelinden girilecek
- Böylece tek uygulama her ortama uyum sağlar

## Proje Yapısı

```
CallCenter.sln
├── src/
│   ├── CallCenter.Api/          → ASP.NET Core Web API + SignalR Hub
│   ├── CallCenter.Web/          → Blazor WebAssembly (tarayıcı client)
│   ├── CallCenter.Shared/       → Ortak modeller, DTO'lar, interface'ler
│   ├── CallCenter.Data/         → EF Core DbContext, migration'lar
│   ├── CallCenter.Windows/      → WPF Blazor Hybrid (masaüstü)
│   └── CallCenter.Mobile/       → MAUI Blazor Hybrid (mobil)
├── docs/
│   ├── yol_haritasi.xml         → Proje yol haritası ve görev takibi
│   └── patterns.md              → Bu dosya
└── microsip-reference/          → MicroSIP C++ kaynak kodu (referans)
```

## Kullanılan Desenler (Patterns)

### Backend
- **PostgreSQL + Npgsql**: Açık kaynak, ücretsiz, performanslı
- **Repository Pattern**: Veritabanı işlemleri soyutlanacak
- **Service Layer**: İş mantığı API controller'lardan ayrılacak
- **JWT Authentication**: Stateless kimlik doğrulama
- **SignalR Hub**: Gerçek zamanlı iletişim merkezi

### Frontend (Blazor)
- **Component-Based Architecture**: Yeniden kullanılabilir UI bileşenleri
- **Shared Razor Class Library**: UI bileşenleri tüm platformlarda paylaşılacak
- **State Management**: Blazor built-in state veya Fluxor

### VoIP/SIP
- **SIP.js / JsSIP**: Tarayıcıda WebRTC üzerinden SIP bağlantısı
- **Adapter Pattern**: Farklı SIP sağlayıcıları aynı interface üzerinden kullanma

## MicroSIP'ten Öğrenilenler

### Dosya Yapısı Analizi
| MicroSIP Dosyası | Ne İşe Yarıyor | Bizde Karşılığı |
|---|---|---|
| `mainDlg.cpp/h` | Ana pencere, çağrı kontrolleri | Agent paneli Blazor component |
| `Calls.cpp/h` | Çağrı yönetimi (yapma, alma, kapatma) | CallService.cs |
| `Contacts.cpp/h` | Rehber yönetimi | ContactService.cs |
| `AccountDlg.cpp/h` | SIP hesap ayarları | SIP ayarları admin paneli |
| `SettingsDlg.cpp/h` | Genel ayarlar | Ayarlar sayfası |
| `Dialer.cpp/h` | Numara çevirici | DialerComponent.razor |
| `MessagesDlg.cpp/h` | Mesajlaşma | (İleride eklenebilir) |
| `Transfer.cpp/h` | Çağrı transferi | TransferService.cs |
| `RinginDlg.cpp/h` | Gelen çağrı bildirimi | IncomingCallComponent.razor |
| `settings.cpp/h` | Ayar okuma/yazma | AppSettings + DB |

### SIP İş Akışı (MicroSIP'ten çıkarılan)
1. Uygulama açılır → SIP hesabına register olur
2. Gelen çağrı → INVITE mesajı alınır → Zil çalar → Kabul/Red
3. Giden çağrı → Numara girilir → INVITE gönderilir → Karşı taraf cevaplar
4. Çağrı sırasında → Bekletme (HOLD), Transfer (REFER), DTMF
5. Çağrı biter → BYE mesajı → Çağrı kaydı oluşturulur

## Geliştirme Günlüğü

### 2026-02-09 - Proje Başlangıcı & Faz 1
- Proje planlaması yapıldı
- MicroSIP C++ kaynak kodu referans olarak indirildi (github.com/pgvee/MicroSIP)
- Yol haritası XML oluşturuldu
- Mimari kararlar belirlendi:
  - Blazor Hybrid ile tek UI, üç platform
  - ASP.NET Core + SignalR backend
  - Dinamik SIP bağlantısı
  - Bootstrap ile UI
  - PostgreSQL veritabanı

**Faz 1 - Temel Altyapı tamamlandı:**
- `CallCenter.slnx` oluşturuldu (.NET 10 yeni slnx formatı)
- 4 proje oluşturuldu: Api, Web, Shared, Data
- **Shared/Entities**: User, CallRecord, Queue, QueueAgent, SipAccount
- **Shared/Enums**: UserRole, AgentStatus, CallDirection, CallStatus
- **Shared/DTOs**: LoginRequest, LoginResponse, AgentStatusUpdate, CallNotification
- **Data/AppDbContext**: PostgreSQL konfigürasyonu, seed data (admin kullanıcı)
- **Api/Services/TokenService**: JWT token üretimi
- **Api/Controllers/AuthController**: Login endpoint
- **Api/Controllers/AgentsController**: Agent CRUD, durum güncelleme
- **Api/Hubs/CallCenterHub**: SignalR hub (bağlantı/kopma yönetimi, durum güncelleme, çağrı bildirimleri)
- **Api/Program.cs**: PostgreSQL, JWT, SignalR, CORS, auto-migration konfigürasyonu
- Build: 0 hata, 0 uyarı

**NuGet Paketleri:**
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0
- Microsoft.EntityFrameworkCore.Tools 10.0.2
- Microsoft.EntityFrameworkCore.Design 10.0.2
- Microsoft.AspNetCore.Authentication.JwtBearer 10.0.2
- BCrypt.Net-Next 4.0.3
- Microsoft.AspNetCore.Components.WebView.Wpf 10.0.10 (Windows projesi)
- Microsoft.AspNetCore.Components.WebView.Maui (Mobile projesi, MAUI built-in)

### 2026-02-09 - Faz 1 Devam: Customer, T(), ID, Windows/Mobile

**Müşteri Yönetimi:**
- Customer entity (firma bilgileri)
- CustomerPersonnel entity (User'a bağlı, [Flags] CustomerPermission ile yetki)
- AuthController'da login'de CustomerPersonnel Include → JWT'ye customer claim'leri

**Çoklu Dil (T() Sistemi):**
- Language, TranslationKey, Translation entity'leri
- ITranslationService + TranslationService (ConcurrentDictionary cache, singleton)
- TranslationsController: JSON get, XML export/import, cache reload
- Seed data: ~28 key, TR/EN çevirileri

**ID Tipi Değişikliği:**
- Tüm entity'lerde `Guid Id` → `int Id` (auto-increment PK)
- Dışarıya açık entity'lerde `Guid Uid` alanı eklendi (URL/link güvenliği)
- FK ilişkileri int tipinde (performans)
- Seed data int ID'lere güncellendi

**Windows ve Mobile Projeleri:**
- CallCenter.Windows: WPF + WebView2 + BlazorWebView (Blazor Hybrid)
- CallCenter.Mobile: .NET MAUI Blazor Hybrid (android, ios, maccatalyst, windows)
- Solution: 6 proje, Build: 0 hata, 0 uyarı

### 2026-02-09 - Faz 2 Başlangıç: Sol Panel ve Layout

**Layout Tasarımı:**
- MainLayout: `app-layout` flex container → Sidebar (fixed) + MainWrapper (margin-left)
- Sidebar: Koyu tema (slate-900 gradient), 260px genişlik, 3 bölüm (header/body/footer)
- TopBar: Beyaz, sticky, hamburger butonu (mobil), durum badge
- Responsive: lg breakpoint (992px), mobil'de sidebar gizli + hamburger ile açılır + overlay

**NavMenu Gruplu Yapı:**
- Dashboard (tek link)
- Arama grubu: Numara Çevirici, Aktif Aramalar, Arama Geçmişi
- Kuyruklar grubu: Kuyruk Listesi, Canlı İzleme
- Raporlar grubu: Arama Raporları, Temsilci Performansı
- Yönetim grubu: Kullanıcılar, Müşteriler, SIP Ayarları, Dil Yönetimi, Sistem Ayarları
- Gruplar açılıp kapanabiliyor (chevron animasyonu)
- Sidebar footer: Agent avatar + isim + rol

**İkon Kütüphanesi:**
- Bootstrap Icons 1.11.3 (CDN)
- İleride lokal dosya olarak da eklenebilir (offline destek)

**Temizlik:**
- Counter.razor ve Weather.razor silindi
- weather.json sample data silindi
- Home.razor → Dashboard sayfasına dönüştürüldü (4 istatistik kartı + Son Aramalar + Temsilci Durumları)

### 2026-02-09 - Faz 2 Devam: Login ve Auth Entegrasyonu

**Login Sayfası:**
- LoginLayout.razor: Sidebar'sız tam ekran, koyu gradient arkaplan
- Login.razor: Form floating input'lar, spinner, hata mesajı gösterimi
- Login.razor.css: Responsive kart, gradient buton, hover efektleri

**Auth Altyapısı (Blazor WASM):**
- JwtAuthStateProvider: JWT token parse (client-side), claim okuma, süre kontrolü
- AuthService: API login çağrısı, localStorage'da token/ad/rol saklama, logout
- AuthHeaderHandler (DelegatingHandler): Her HTTP isteğine otomatik Bearer token ekleme
- RedirectToLogin component: Yetkisiz kullanıcıları /login'e yönlendirme

**App.razor Güncellendi:**
- CascadingAuthenticationState → tüm component'lara auth state akıyor
- AuthorizeRouteView → yetkisiz sayfalar RedirectToLogin tetikliyor
- Home.razor'a [Authorize] attribute eklendi

**Layout Auth Entegrasyonu:**
- MainLayout: Logout butonu (üst bar, sağ taraf)
- NavMenu sidebar footer: JWT'den okunan ad ve rol (Türkçe rol adları)

**NuGet (Web projesine):**
- Microsoft.AspNetCore.Components.Authorization 10.0.2
- System.IdentityModel.Tokens.Jwt 8.7.0

**Seed Data Düzeltmesi:**
- BCrypt.HashPassword() her seferinde farklı hash üretiyordu → EF Core PendingModelChangesWarning
- Çözüm: Hash bir kez üretilip sabit string olarak seed data'ya kondu
- Admin şifresi: 1123Azs+- (güvenlik kaybı yok — BCrypt.Verify salt'ı hash'ten okur)

**İlk Migration:**
- `dotnet ef migrations add InitialCreate` başarılı
- `dotnet ef database update` → CallCenterDB oluşturuldu, tüm tablolar + seed data hazır

## Proje Vizyonu — ÖNEMLİ HATIRLATMA

**Bu proje bir SOFTPHONE uygulamasıdır!** MicroSIP'in (https://www.microsip.org/) çok müşterili,
çağrı merkezi versiyonunu yazıyoruz. Asıl hedef:
- Gerçek SIP/WebRTC üzerinden sesli arama yapma/alma
- Windows'ta native SIP (WPF), Web'de SIP.js/WebRTC, Mobil'de SRTP/VoIP
- Admin panelinden SIP sunucu bilgileri girilecek (dinamik SIP)
- MicroSIP C++ kaynak kodu referans: `microsip-reference/` klasöründe
- Faz 3-5'te VoIP entegrasyonu gelecek — şu an UI ve altyapı hazırlanıyor

**Platform = Kullanıcı Profili Eşlemesi:**
| Platform | Kullanıcı | Senaryo |
|---|---|---|
| Web (Blazor WASM) | Ofis çağrı merkezi temsilcisi | Masabaşında, kulaklıkla, kuyruktan arama cevaplıyor |
| Windows (WPF Hybrid) | Ofis temsilcisi / Supervisor | Native masaüstü, system tray, kısayollar |
| **Mobil (MAUI Hybrid)** | **Saha elemanı (kurye, teknisyen, satışçı)** | **Dışarıda, müşterinin müşterilerini firmamız üzerinden arıyor** |

**Mobil = Saha Personeli Uygulaması**: Müşterilerimizin kuryesi, teknik servisi vb. saha ekibi
bu uygulamayı kullanarak teslimat/randevu yapacağı kişileri arar. Tüm aramalar firma üzerinden
geçer → kayıt altında, raporlanabilir, denetlenebilir. UI hızlı arama, tek elle kullanım odaklı olacak.

### 2026-02-09 - Görev 2.2: Agent Paneli + TypeItem Dönüşümü

**TypeItem Pattern (Enum Yerine):**
- **KURAL**: Standart C# enum kullanılmayacak, TypeItem pattern kullanılacak. Hiçbir [Flags] enum kalmadı.
- TypeItem base class: Id, SystemName, NameResourceKey, Description, Icon, CssClass, DisplayOrder, IsDefault, IsActive, IsSystem
- TypeDefinitions.cs: UserRoles, AgentStatuses, CallStatuses, CallDirections — hepsi tek dosyada
- Her TypeItem'da inner `Ids` class (const int) — EF Core seed data ve koşullarda kullanılır
- Entity'lerde `int RoleId`, `int StatusId`, `int DirectionId` — enum yerine int
- Referans: SecretCustomer projesindeki TypeItem deseni

**Enum → TypeItem Dönüşümü:**
- UserRole enum → UserRoles static class (Agent=1, Supervisor=2, Admin=3, CustomerUser=4)
- AgentStatus enum → AgentStatuses static class (Offline=1, Available=2, Busy=3, OnBreak=4, InCall=5, AfterCallWork=6)
- CallStatus enum → CallStatuses static class (Ringing=1, InProgress=2, OnHold=3, Transferred=4, Completed=5, Missed=6, Failed=7)
- CallDirection enum → CallDirections static class (Inbound=1, Outbound=2)
- Eski enum dosyaları silindi: UserRole.cs, AgentStatus.cs, CallStatus.cs, CallDirection.cs
- Tüm backend ve frontend referansları güncellendi

**SignalR Client (Web):**
- NuGet: Microsoft.AspNetCore.SignalR.Client 10.0.2
- HubService.cs: JWT token ile bağlantı, WithAutomaticReconnect (0s, 2s, 5s, 10s, 30s)
- Event'ler: OnAgentStatusChanged, OnIncomingCall, OnCallEnded, OnConnectionStateChanged
- Metodlar: ConnectAsync, DisconnectAsync, UpdateStatusAsync, NotifyIncomingCallAsync, NotifyCallEndedAsync

**TopBar Durum Dropdown:**
- MainLayout'ta statik badge → tıklanabilir dropdown (4 seçenek: Müsait, Meşgul, Mola, Çevrimdışı)
- SignalR event'i ile senkron (hub bağlantısında otomatik Available)
- Icon ve cssClass TypeItem'dan okunur
- WiFi-off göstergesi: SignalR bağlantısı kesilirse uyarı

**Agent Sayfaları:**
- Dialer (Numara Çevirici): /dialer — Tuş takımı, numara input, arama başlat/beklet/kapat, timer
- Active Calls (Aktif Aramalar): /calls/active — SignalR'dan gelen aktif arama kartları, cevapla/reddet/beklet/transfer/kapat
- Call History (Arama Geçmişi): /calls/history — API'den gelen geçmiş, filtreleme (yön/durum/numara arama)

**Gelen Arama Bildirimi:**
- IncomingCallNotification.razor: Global overlay popup, animasyonlu çalan telefon ikonu
- Kabul/Reddet butonları, kuyruk bilgisi gösterimi
- MainLayout'a yerleştirildi (tüm sayfalarda aktif)

**API Endpoint'leri (CallsController):**
- GET /api/calls/history — Sayfalı arama geçmişi
- GET /api/calls/active — Aktif aramalar
- POST /api/calls/start — Yeni arama başlat
- PUT /api/calls/{id}/hold — Aramayı beklet
- PUT /api/calls/{id}/end — Aramayı sonlandır
- PUT /api/calls/{id}/answer — Aramayı cevapla

**NuGet (Web):**
- Microsoft.AspNetCore.SignalR.Client 10.0.2

**Migration yeniden oluşturuldu** (DB drop + InitialCreate + apply)

### 2026-02-09 - Dinamik Müşteri Yetki Sistemi

**Mimari Karar: Katmanlı Yetki Sistemi**
- **1. Katman (TypeItem)**: Yetki tipleri kodda sabit tanımlı → Derleme zamanı güvenliği
- **2. Katman (CustomerPortalModule DB tablosu)**: Müşteriye hangi modüller açık → Lisans/paket yönetimi
- **3. Katman (CustomerPersonnelPermission DB tablosu)**: Personele granüler yetki atama → Dinamik yönetim
- Supervisor ve Admin'ler yönetir, personele sadece müşterinin açık modüllerindeki yetkiler atanabilir

**[Flags] CustomerPermission enum SİLİNDİ** — Yerine dinamik sistem geldi.

**Yeni TypeItem Grupları (TypeDefinitions.cs):**
- `PortalModules` — 7 modül: Dashboard, Calls, Reports, Agents, Queues, Settings, Personnel
  - `IsDefault` ile yeni müşteriye varsayılan açılacak modüller işaretli (Dashboard, Calls, Personnel)
- `CustomerPermissionTypes` — 15 yetki tipi, 7 modüle dağılmış (ID aralıkları: 1-9 Dashboard, 10-19 Call, 20-29 Report, 30-39 Agent, 40-49 Queue, 50-59 Settings, 60-69 Personnel)
  - `GetByModule(int moduleId)` — Modüle göre yetkiler
  - `GetModuleId(int permissionTypeId)` — Yetki tipinden modül ID'sini bul
- `PermissionScopes` — 3 kapsam: All(1), Own(2), Customer(3 - varsayılan)

**Yeni Entity'ler:**
- `CustomerPortalModule` — Müşteriye açık modüller (CustomerId + ModuleId unique index)
- `CustomerPersonnelPermission` — Personel yetkileri (PersonnelId + PermissionTypeId unique index, CreatedByUserId ile iz takibi, ValidFrom/ValidUntil ile süre kontrolü)

**Güncel Entity Değişikliği:**
- `CustomerPersonnel.Permissions`: `CustomerPermission Permissions` (int bitmask) → `ICollection<CustomerPersonnelPermission> Permissions` (nav property)
- `Customer.PortalModules`: Yeni navigation property eklendi

**JWT Token Değişikliği:**
- Eski: `CustomerPermissions: "7"` (bitmask int)
- Yeni: `CustomerPermissions: "1,10,11,20,30"` (virgülle ayrılmış aktif yetki TypeId'leri)
- Login'de tarih kontrolü yapılıyor (ValidFrom/ValidUntil)

**API Endpoint'leri (CustomerPermissionsController):**
- Modül yönetimi:
  - GET /api/customers/{id}/modules — Müşteri modüllerini listele
  - POST /api/customers/{id}/modules — Modül aç (toplu)
  - DELETE /api/customers/{id}/modules/{moduleId} — Modül kapat
- Yetki tipleri:
  - GET /api/customers/{id}/permissions/types — Açık modüllerdeki yetki tiplerini listele
- Personel yetki yönetimi:
  - GET /api/customers/{id}/personnel/{pid}/permissions — Personel yetkilerini getir
  - POST /api/customers/{id}/personnel/{pid}/permissions — Toplu yetki ata
  - PUT /api/customers/{id}/personnel/{pid}/permissions/{id} — Yetki güncelle
  - DELETE /api/customers/{id}/personnel/{pid}/permissions/{id} — Yetki kaldır

**Migration yeniden oluşturuldu** (DB drop + InitialCreate + apply)
**Build: 0 hata, 0 uyarı**

### 2026-02-09 - Görev 2.3: Supervisor Dashboard

**Supervisor Dashboard — Firma Bazlı Gerçek Zamanlı:**
- Dashboard verileri firma bazlı filtreli: Supervisor üstte firma dropdown'dan seçer, "Tümü" genel bakış. Admin tümünü görür.
- Tek endpoint `GET /api/supervisor/dashboard?customerId=X` tüm veriyi döndürür (KPI + agent listesi + son aramalar + kuyruk özeti)
- `GET /api/supervisor/customers` — Firma dropdown için müşteri listesi
- `[Authorize(Roles = "Admin,Supervisor")]` — Sadece yetkili roller erişebilir

**Dashboard Bölümleri:**
- KPI kartları: Aktif aramalar, müsait temsilci, kuyrukta bekleyen, bugün toplam
- Son aramalar tablosu: Son 10 arama, yön, numara, temsilci, durum, süre
- Temsilci durum listesi: Tüm agent'lar, anlık durum badge, çağrıdaysa ikon
- Alt istatistikler: Cevaplanan, kaçırılan, ortalama süre
- Kuyruk özeti tablosu: Kuyruk adı, bekleyen, aktif, temsilci sayısı

**SignalR Gerçek Zamanlı Güncelleme:**
- `OnAgentStatusChanged` → Temsilci listesinde anlık güncelleme
- `OnIncomingCall` → KPI kartlarında aktif arama ve kuyruk sayısı artırılır
- `OnCallEnded` → KPI kartlarında aktif arama düşürülür, toplam artırılır
- 60 saniyede bir fallback tam yenileme (SignalR event kaçırılırsa)

**Firma Bazlı Filtreleme Mantığı:**
- customerId verilmişse: CustomerPersonnel → User ID'leri bulunur → Sadece o agent'ların verileri
- customerId verilmemişse: Tüm Users (IsActive), tüm CallRecords, tüm Queues
- Queue ve SipAccount artık Customer'a bağlı (CustomerId FK)

**Build: 0 hata, 0 uyarı**

### 2026-02-09 - Queue ve SipAccount → Customer İlişkisi

**Temel multi-tenant düzeltme:**
- `Queue.CustomerId` FK eklendi — Her kuyruk bir müşteriye ait
- `SipAccount.CustomerId` FK eklendi — Her SIP hesabı bir müşteriye ait
- `Customer.Queues` ve `Customer.SipAccounts` navigation property'leri eklendi
- Queue unique index: `(CustomerId, Name)` — Aynı firma içinde kuyruk adı tekil
- Cascade delete: Müşteri silinince kuyruğu ve SIP hesapları da silinir
- SupervisorController'da kuyruk sorgusu firma bazlı filtreleme destekliyor
- Migration yeniden oluşturuldu (InitialCreate)

**Build: 0 hata, 0 uyarı**

### 2026-02-09 - SIP/VoIP Multi-Tenant Fizibilite Araştırması

**Soru:** Birden fazla müşteri farklı SIP sağlayıcılarına bağlanacak — her müşteriye ayrı SIP bağlantısı kurulabilir mi?

**Cevap: EVET — Engelleyen bir durum yok.**

**Web (Tarayıcı — SIP.js / JsSIP):**
- SIP.js ve JsSIP birden fazla `UserAgent` instance'ı destekler
- Her müşterinin SIP hesabı için ayrı UserAgent oluşturulur
- WebSocket üzerinden bağlantı (WSS)
- Tüm büyük sağlayıcılar WebRTC/WebSocket destekliyor

**Windows (Masaüstü — SIPSorcery):**
- SIPSorcery: Pure C# .NET kütüphanesi, NuGet paketi
- Birden fazla `SIPRegistrationUserAgent` aynı anda çalışabilir
- BSD lisansı (Ocak 2026'dan itibaren), ticari kullanıma uygun
- Alternatif: pjsip C wrapper da mümkün ama SIPSorcery daha temiz

**Cloud Sağlayıcılar Uyumluluğu:**
| Sağlayıcı | WebRTC/WebSocket | Adapter Yaklaşımı |
|---|---|---|
| **Asterisk** | ✅ Dahili WebSocket (mod_http_websocket) | Doğrudan SIP.js bağlantısı |
| **FreeSWITCH** | ✅ mod_verto + WSS | Doğrudan SIP.js/Verto bağlantısı |
| **Telnyx** | ✅ telnyx-webrtc SDK | En iyi uyum — SIP.js tabanlı SDK |
| **Twilio** | ✅ twilio.js SDK | Kendi SDK'sı, Voice API üzerinden |
| **Vonage** | ✅ vonage-client-sdk | WebRTC tabanlı |
| **Plivo** | ✅ plivo-browser-sdk | WebRTC tabanlı |
| **Bandwidth** | ✅ WebRTC desteği | SIP üzerinden |
| **VoIP.ms** | ❌ WebSocket yok | Kamailio proxy gerekli (SIP→WSS) |

**Faz 3 Öncelik Sırası (Önerilen):**
1. Asterisk / FreeSWITCH doğrudan bağlantı (en yaygın, on-premise)
2. Telnyx SDK entegrasyonu (SIP.js tabanlı, kolay geçiş)
3. Twilio SDK entegrasyonu (yaygın, iyi dokümantasyon)
4. Diğer cloud sağlayıcılar (Vonage, Plivo, Bandwidth)
5. Legacy sağlayıcılar — Kamailio proxy üzerinden (VoIP.ms vb.)

**Adapter Pattern Doğrulandı:**
- `ISipProvider` interface → Her sağlayıcı için concrete implementation
- Web: `AsteriskProvider`, `TelnyxProvider`, `TwilioProvider` vb.
- Windows: SIPSorcery ile doğrudan SIP, sağlayıcıdan bağımsız
- DB'deki `SipAccount.Server/Port/Username/Password/Transport` bilgileri yeterli
- Ek sağlayıcı-özel config gerekirse `SipAccount`'a JSON metadata alanı eklenebilir

**Sonuç:** Multi-tenant SIP bağlantısı teknik olarak sorunsuz. Adapter pattern yaklaşımımız doğru.

## Dış Entegrasyon Vizyonu (Faz 7)

Bu proje izole bir uygulama değil. Dış dünya ile iki yönlü bağlantı kuracak:
- **Outbound** (biz → dış sistemler): CRM, ERP, Helpdesk sistemlerine bağlanma (Salesforce, SAP, Zendesk vb.)
- **Inbound** (dış sistemler → biz): REST API, Webhook, SignalR ile dış sistemlerin bize erişimi
- **Mevcut uyum**: Halihazırda kullanılan takip uygulamalarından veri göçü, paralel çalışma desteği
- **Teknik**: Adapter/Connector pattern, API Key/OAuth2 güvenlik, Swagger/OpenAPI dokümantasyon, SDK

### 2026-02-10 - Görev 2.4: Admin Paneli (Tam CRUD)

**8 Adımda Tamamlandı:**

**Adım 1 — Ortak Bileşenler:**
- `Pagination.razor` — Sayfalama bileşeni (CurrentPage, TotalPages, OnPageChanged, ellipsis desteği)
- `ConfirmDialog.razor` — Bootstrap modal silme onayı (Show/Hide metodları)
- `ToastNotification.razor` — Sağ üst köşe bildirim (4 saniye otomatik kapanma)
- `ToastService.cs` — ShowSuccess/ShowError/ShowWarning/ShowInfo event servisi, DI'a eklendi

**Adım 2 — Kullanıcı Yönetimi (Pattern-setter CRUD):**
- `AdminDtos.cs` — Tüm admin DTO'ları tek dosyada: PagedResult<T>, UserListDto/CreateDto/UpdateDto, CustomerListDto/CreateDto/UpdateDto/DetailDto, QueueListDto/CreateDto/UpdateDto/DetailDto/AgentDto/AgentAssignDto, SipAccountListDto/CreateDto/UpdateDto, TranslationKeyListDto/CreateDto/UpdateDto, LanguageDto, SystemSettingDto/CreateDto/UpdateDto
- `UsersController.cs` — [Authorize(Roles="Admin")], GET(sayfalı+arama+rol filtre), GET/{id}, POST(BCrypt hash, unique), PUT(şifre opsiyonel), DELETE(soft, admin kendini silemez)
- `Users.razor` — Tablo + filtreler + UserForm modal + ConfirmDialog + Pagination
- `UserForm.razor` — Create/Edit modal, EditForm + DataAnnotationsValidator

**Adım 3 — Müşteri Yönetimi:**
- `CustomersController.cs` — [Authorize(Roles="Admin,Supervisor")], varsayılan portal modülleri otomatik atama, personel/kuyruk/SIP sayıları
- `Customers.razor` + `CustomerForm.razor` — Aynı pattern

**Adım 4 — Kuyruk Yapılandırması:**
- `QueuesController.cs` — Firma bazlı, unique(customerId+name), POST/DELETE agents endpoint'leri
- `Queues.razor` — Firma dropdown, agent atama modal paneli (badge'larla gösterim, ekleme/çıkarma)
- `QueueForm.razor` — Firma seçimi + kuyruk ayarları

**Adım 5 — SIP Hesap Ayarları:**
- `SipAccountsController.cs` — [Authorize(Roles="Admin")], Password listede dönmez, update'de null ise değişmez, IsDefault tek
- `SipAccounts.razor` + `SipAccountForm.razor` — Transport dropdown (UDP/TCP/TLS/WSS), SRTP checkbox

**Adım 6 — Dil Yönetimi:**
- `TranslationsController.cs`'a eklendi: GET keys(sayfalı+arama+modül), GET languages, POST/PUT/DELETE keys — her işlemde cache otomatik yenilenir
- `Translations.razor` — Modül filtresi, her dil sütun olarak gösterilir, XML export butonu, cache yenile butonu
- `TranslationForm.razor` — Key + her dil için input alanı

**Adım 7 — Sistem Ayarları:**
- `SystemSetting.cs` entity — Key, Value, Group, ValueType, Description, IsSystem
- `SettingsController.cs` — GET(grup filtre), PUT, POST, DELETE(IsSystem engelle)
- `Settings.razor` — Grup bazlı kartlar (Genel/Güvenlik/SIP/Bildirim), inline edit, yeni ayar modal
- `AppDbContext.cs` — SystemSettings DbSet + OnModelCreating + 14 seed ayar
- Migration: `AddSystemSettings`

**Adım 8 — Role-Based Menü Filtreleme:**
- NavMenu güncellendi: `IsAdmin`, `IsAdminOrSupervisor`, `IsCustomerUser` property'leri
- Yönetim grubu: Sadece Admin+Supervisor görür
- Kullanıcılar, SIP, Dil, Sistem Ayarları: Sadece Admin görür
- Müşteriler, Kuyruk Yapılandırması: Admin+Supervisor görür
- Arama/Kuyruklar/Raporlar: CustomerUser hariç herkes görür
- Dashboard: Herkes görür
- Yönetim grubuna `/admin/queues` linki eklendi

**Build: 0 hata, 0 uyarı (API + Web)**

**Razor Build Hataları ve Çözümleri:**
- Pagination'da `page` değişken adı `@page` Razor directive ile çakıştı → `pageNum` olarak değiştirildi
- TranslationForm'da inline lambda'daki `""` (boş string literal) Razor parser'ı bozdu → `HandleValueChange` metodu ile çözüldü

### 2026-02-10 - Public Landing Page (Tanıtım Sayfası)

**Değişiklik:**
- Home.razor route `"/"` → `"/dashboard"` olarak değiştirildi (dashboard artık /dashboard'da)
- Login.razor post-login yönlendirmesi `"/"` → `"/dashboard"` olarak güncellendi
- NavMenu Dashboard linki `""` → `"dashboard"` olarak güncellendi
- **Yeni:** `Index.razor` — `/` route, LoginLayout kullanan public tanıtım sayfası
  - Hero bölümü: Logo, başlık, alt başlık, "Giriş Yap" butonu
  - 6 özellik kartı: VoIP/SIP, Kuyruk, Dashboard, Çoklu Müşteri, Mobil, Güvenlik
  - 3 platform kartı: Web, Windows, Mobil
  - 4 rol kartı: Admin, Supervisor, Agent, Müşteri
  - Footer
  - Zaten giriş yapmışsa otomatik /dashboard'a yönlendirme
- `Index.razor.css` — Koyu tema, glassmorphism kartlar, responsive grid (3→2→1 kolon)
- Build: 0 hata, 0 uyarı

### 2026-02-10 - Faz 2 Tamamlama: Kuyruk ve Rapor Sayfaları (4 Sayfa)

**DTO'lar:**
- `ReportDtos.cs` — Yeni dosya: CallReportResponse, CallReportItemDto, AgentReportResponse, AgentReportItemDto
- `SupervisorDtos.cs` — 3 DTO eklendi: MyQueueDto, QueueLiveDto, QueueLiveAgentDto

**Backend Endpoint'leri:**
- `AgentsController.cs` — `GET /api/agents/my/queues` eklendi: Agent kendi kuyrukları, Admin/Supervisor tümü (customerId filtreli)
- `SupervisorController.cs` — `GET /api/supervisor/queues/live?customerId=X` eklendi: Her kuyruğun anlık durumu + agent listesi
- `ReportsController.cs` — Yeni controller: [Authorize(Roles="Admin,Supervisor")]
  - `GET /api/reports/calls?customerId&from&to&directionId&statusId&page&pageSize` — Özet istatistikler + sayfalamalı arama listesi
  - `GET /api/reports/agents?customerId&from&to&page&pageSize` — Özet istatistikler + sayfalamalı temsilci performansı

**Kuyruk Listesi (/queues):**
- `Queues/Index.razor` — [Authorize], Agent: kendi kuyrukları, Admin/Supervisor: tümü + firma dropdown
- Read-only tablo: Kuyruk Adı, Firma, Bekleyen, Aktif, Temsilci Sayısı, Durum badge
- `Queues/Index.razor.css`

**Canlı Kuyruk İzleme (/queues/live):**
- `Queues/Live.razor` — [Authorize(Roles="Admin,Supervisor")]
- Firma dropdown + card-based layout (her kuyruk = 1 kart)
- Kart içinde: Kuyruk adı, firma, bekleyen/aktif badge, agent listesi (durum rengine göre badge)
- SignalR: OnAgentStatusChanged, OnIncomingCall, OnCallEnded → UI güncelle
- 30 saniye fallback timer ile tam yenileme
- `Queues/Live.razor.css` — Card hover efekti, agent badge stilleri

**Arama Raporları (/reports/calls):**
- `Reports/Calls.razor` — [Authorize(Roles="Admin,Supervisor")]
- Filtreler: Firma dropdown + tarih aralığı (input type="date") + yön select + durum select
- Özet kartları: Toplam Arama, Cevaplanan, Cevapsız, Ort. Süre
- Tablo + Pagination: Arayan, Aranan, Yön, Durum, Süre, Temsilci, Kuyruk, Tarih
- Varsayılan: Son 7 gün
- `Reports/Calls.razor.css`

**Temsilci Performansı (/reports/agents):**
- `Reports/Agents.razor` — [Authorize(Roles="Admin,Supervisor")]
- Filtreler: Firma dropdown + tarih aralığı
- Özet kartları: Toplam Temsilci, En İyi Performans, En Düşük Ort. Süre, Genel Ort. Süre
- Tablo + Pagination: Temsilci, Dahili, Toplam, Cevaplanan, Cevapsız, Ort. Süre, Cevaplama Oranı (%)
- Oran renklendirme: ≥80% yeşil, ≥50% sarı, <50% kırmızı
- Varsayılan: Son 30 gün
- `Reports/Agents.razor.css`

**Build: 0 hata, 0 uyarı (Tüm solution — API + Web + Shared + Data + Windows + Mobile)**

### 2026-02-10 - Faz 3: SIP/VoIP Entegrasyonu (Tam Faz)

**8 Adımda Tamamlandı:**

**Adım 1 — SIP.js Altyapısı:**
- `index.html` — SIP.js 0.21.2 CDN, `<audio id="remoteAudio" autoplay>`
- `wwwroot/js/sipClient.js` — Tam SIP.js UserAgent API wrapper (initialize, makeCall, answerCall, hangup, holdCall, unholdCall, sendDtmf, transferCall, getAudioDevices, setAudioDevice, dispose + C# callback köprüsü)

**Adım 2 — C# SipService (Blazor JS Interop):**
- `Services/SipService.cs` — IJSRuntime wrapper, DotNetObjectReference, event'ler, [JSInvokable] callback'ler, AudioDeviceInfo DTO
- `Program.cs` — `AddScoped<SipService>()` DI kaydı

**Adım 3 — SIP Config API:**
- `SipConnectionDto.cs` — SipConnectionInfoDto (WsUri, SipUri, AuthUsername, AuthPassword, DisplayName, Transport, UseSrtp)
- `SipAccountsController.cs` — `GET /api/sipaccounts/my/connection` (tüm roller, JWT CustomerId claim, default SIP hesabı, WSS URI otomatik oluşturma)
- Controller auth refactor: Class [Authorize(Roles="Admin")] → [Authorize] + method seviyesinde [Authorize(Roles="Admin")]

**Adım 4 — Dialer.razor SIP Entegrasyonu:**
- Tamamen yeniden yazıldı: SipService inject, API'den SIP init, MakeCallAsync, HoldAsync/UnholdAsync toggle, HangupAsync, DTMF gönderimi, SIP kayıt badge, event-driven timer
- CSS: call-resume + sip-badge stilleri

**Adım 5 — Gelen Arama:**
- `MainLayout.razor` — SIP register başlatma, AudioSettings component, IAsyncDisposable
- `IncomingCallNotification.razor` — SipService.OnIncomingCall dinleme, isSipCall flag, SIP accept/reject
- `Active.razor` — SIP aksiyonları (answer, hold, resume, end) + TransferDialog entegrasyonu

**Adım 6 — Transfer + Ses Cihazı:**
- `TransferDialog.razor` — Blind transfer modal, SipService.TransferAsync
- `AudioSettings.razor` + CSS — Topbar ses cihazı seçici, mikrofon/hoparlör listesi, setSinkId

**Adım 7 — Kuyruk ACD Sistemi:**
- `CallDistributionService.cs` — Uygulama seviyesi ACD (QueueAgent öncelik + en az meşgul strateji), SignalR Clients.User ile agent'a bildirim
- `CallsController.cs` — `POST /api/calls/incoming` (arama kaydı + ACD yönlendirme)
- `CallCenterHub.cs` — `NotifySpecificAgent` metodu
- `Program.cs` (API) — `AddScoped<CallDistributionService>()` DI kaydı

**Yeni Dosyalar (6):**
1. `wwwroot/js/sipClient.js`
2. `Services/SipService.cs`
3. `DTOs/SipConnectionDto.cs`
4. `Components/TransferDialog.razor`
5. `Components/AudioSettings.razor` + CSS
6. `Api/Services/CallDistributionService.cs`

**Düzenlenen Dosyalar (9):**
1. `wwwroot/index.html`
2. `Pages/Dialer.razor` + CSS
3. `Layout/MainLayout.razor`
4. `Components/IncomingCallNotification.razor`
5. `Pages/Calls/Active.razor`
6. `Controllers/SipAccountsController.cs`
7. `Controllers/CallsController.cs`
8. `Hubs/CallCenterHub.cs`
9. `Api/Program.cs` + `Web/Program.cs`

**Build: 0 hata, 0 uyarı (Tüm solution)**

### 2026-02-10 - Faz 4: Müşteri Portalı + Modül Yönetimi

**Mimari Karar: Factory + Service Pattern (Tüm Kod İçin)**
- Tüm controller'lar: Controller → ServiceFactory → IXxxService → AppDbContext
- Controller'larda iş mantığı YOK, sadece HTTP routing
- Tüm iş mantığı (unique kontrol, BCrypt hash, yetki kopyalama) service katmanında
- ServiceFactory: IServiceProvider wrapper, `CreatePortalService()` ile DI'dan resolve
- Bu sayede controller'lar ince, test edilebilir, service'ler bağımsız

**Sektörel Araştırma (Call Center Modül Yapısı):**
- Freshcaller, Zendesk Talk, Five9, Genesys Cloud, 3CX, Talkdesk, NICE CXone, Aircall, RingCentral incelendi
- Sonuç: Mevcut 7 modülümüz yetersiz, 7 yeni modül eklendi (toplam 14)
- Paketleme vizyonu: Starter (3), Professional (10), Enterprise (14)

**7 Yeni Modül (ID 8-14, IsDefault=false):**
| ID | Modül | Açıklama |
|----|-------|----------|
| 8 | UserTypes | Müşteri tanımlı rol şablonları |
| 9 | SipSettings | SIP/VoIP yapılandırması |
| 10 | CallRecords | Arama kaydı dinleme/yönetimi |
| 11 | QualityManagement | Kalite değerlendirme formları |
| 12 | KnowledgeBase | Bilgi bankası, agent senaryoları |
| 13 | Integrations | API/webhook/CRM entegrasyonları |
| 14 | Campaigns | Giden arama kampanyaları |

**17 Yeni İzin (CustomerPermissionTypes):**
- UserTypes: UserTypeView(70), UserTypeManage(71)
- SipSettings: SipView(80), SipManage(81)
- CallRecords: RecordListen(90), RecordDownload(91), RecordDelete(92)
- QualityManagement: QualityView(100), QualityManage(101), QualityScore(102)
- KnowledgeBase: KBView(110), KBManage(111)
- Integrations: IntegrationView(120), IntegrationManage(121)
- Campaigns: CampaignView(130), CampaignManage(131), CampaignExecute(132)

**Yeni Entity'ler:**
- `CustomerUserType` — Müşteri tanımlı rol şablonu (Id, Uid, CustomerId, Name, Description, IsActive)
- `CustomerUserTypePermission` — Tip-yetki ilişkisi (UserTypeId, PermissionTypeId, unique index)
- `CustomerPersonnel.UserTypeId` — FK eklendi (SetNull on delete)

**Service Katmanı:**
- `IPortalService` — 16 metod (dashboard, usertypes CRUD+perms, personnel CRUD+perms, modules, SIP)
- `PortalService` — Tam implementasyon, AppDbContext injection
- `ServiceFactory` — IServiceProvider wrapper
- DI: `AddScoped<IPortalService, PortalService>()` + `AddScoped<ServiceFactory>()`

**PortalController (16 Endpoint):**
- [Authorize(Roles = "Admin,CustomerUser")]
- `ResolveCustomerId()` — Admin: ?customerId param, CustomerUser: JWT claim
- `HasPermission()` — Admin always true, CustomerUser: JWT CustomerPermissions claim
- Dashboard, UserTypes CRUD+perms, Personnel CRUD+perms, Modules (read-only), SIP (read+limited update)

**Client-Side PermissionService:**
- JWT'deki CustomerPermissions claim'ini parse eder
- `HasPermission(int)`, `HasModule(int)`, `IsAdmin` property'leri
- NavMenu'de dinamik portal menüsü gösteriminde kullanılır

**Portal Sayfaları (8 Razor):**
1. Dashboard.razor — KPI kartları + modül listesi
2. UserTypes.razor — Tablo + yetki atama modal (modül bazlı gruplandırma)
3. UserTypeForm.razor — Create/Edit modal
4. Personnel.razor — Tablo + yetki/düzenle/deaktive butonları
5. PersonnelForm.razor — Create/Edit modal (tek FormModel pattern)
6. PersonnelPermissionForm.razor — Personel yetki atama modal
7. Modules.razor — Read-only kart görünümü
8. SipSettings.razor — Tablo + sınırlı edit modal (Server/Port read-only)

**NavMenu Güncellemesi:**
- CustomerUser: Portal grubu (Personnel, UserTypes, SIP, Moduller) + Arama + Raporlar
- Admin/Supervisor/Agent: Mevcut menü aynen korunur
- Müşteri tablosuna "Portal" butonu eklendi → /portal/dashboard?customerId={id}

**Build Hataları ve Çözümleri:**
1. `Microsoft.AspNetCore.WebUtilities` Blazor WASM'de YOK → `System.Web.HttpUtility.ParseQueryString()` kullanıldı
2. Razor'da `@bind-Value` ternary expression çalışmaz → Tek FormModel pattern kullanıldı

**Yeni Dosyalar (17):** Entity(2), DTO(1), Service(3), Controller(1), Client Service(1), Razor(8), Migration(1)
**Düzenlenen Dosyalar (8):** TypeDefinitions, CustomerPersonnel, Customer, AppDbContext, Api/Program, Web/Program, NavMenu, Customers

**Build: 0 hata, 0 uyarı (Tüm solution)**

### 2026-02-10 - Eski Controller'ları Factory+Service Mimarisine Geçirme

**Tüm 12 eski controller (Faz 1-3) Factory+Service pattern'e geçirildi.**

**11 Yeni Interface (Services/Interfaces/):**
- IAuthService, IUserService, ICustomerService, IAgentService, ICallService
- IQueueService, ISipAccountService, ISupervisorService, IReportService
- ISettingService, ITranslationManagementService

**11 Yeni Service (Services/):**
- AuthService (TokenService + IConfiguration inject)
- UserService (BCrypt hash, unique kontrol, soft delete)
- CustomerService (Müşteri CRUD + CustomerPermissions modül/yetki yönetimi)
- AgentService (SignalR IHubContext inject, durum güncelleme)
- CallService (SignalR + CallDistributionService inject, ACD yönlendirme)
- QueueService (Kuyruk CRUD, agent atama/çıkarma)
- SipAccountService (SIP CRUD, IsDefault yönetimi, bağlantı bilgisi)
- SupervisorService (Dashboard aggregation, canlı kuyruk, müşteri listesi)
- ReportService (Arama raporu, agent performansı)
- SettingService (Sistem ayarları CRUD, IsSystem koruma)
- TranslationManagementService (Çeviri CRUD, XML import/export, cache yönetimi)

**Domain Bazlı Servis Birleştirme:**
- CustomersController + CustomerPermissionsController → ICustomerService (tek servis)
- Mevcut ITranslationService (cache/lookup) dokunulmadı → ITranslationManagementService (CRUD)
- Mevcut CallDistributionService ve TokenService dokunulmadı, yeni servisler bunları inject ediyor

**Inline DTO Taşıma:**
- CallsController'daki StartCallRequest + IncomingCallRequest → Shared/DTOs/CallDtos.cs

**ServiceFactory Güncelleme:**
- 12 yeni Create*Service() metodu eklendi (mevcut CreatePortalService korundu)

**Program.cs DI Kayıtları:**
- 11 yeni AddScoped<IXxxService, XxxService>() kaydı

**12 Controller Dönüşümü:**
- Tüm controller'lar: AppDbContext → ServiceFactory
- İş mantığı sıfır, sadece HTTP routing + request/response mapping
- Aynı route, aynı endpoint imzası, aynı HTTP status kodları

**Dosya Özeti:**
- Yeni dosyalar (23): 11 interface + 11 service + 1 DTO (CallDtos.cs)
- Düzenlenen dosyalar (14): 12 controller + ServiceFactory.cs + Program.cs

**Build: 0 hata, 0 uyarı (Tüm solution — API + Web + Shared + Data + Windows + Mobile)**

### 2026-02-10 - Proje Altyapı Durum Özeti

**Tamamlanan Fazlar:**
- Faz 1: Temel Altyapı (8 görev) — Solution, entity, JWT, SignalR, müşteri, i18n, ID dönüşümü
- Faz 2: Web Arayüzü (7 görev) — Layout, login, agent paneli, admin paneli, kuyruk/rapor sayfaları
- Faz 3: SIP/VoIP Entegrasyonu (4 görev) — SIP.js, WebRTC, ACD, transfer, ses cihazı
- Faz 4: Müşteri Portalı + Modül Yönetimi (8 görev) — Portal, 14 modül, Factory+Service pattern
- Görev 4.8: Tüm controller'lar Factory+Service'e geçirildi

**Sayısal Özet:**
- 6 proje (Api, Web, Shared, Data, Windows, Mobile)
- 13 controller, 12 service, 12 interface, ServiceFactory
- ~30+ Razor sayfa, ~60 API endpoint
- 14 portal modülü, 32 izin tipi
- 0 build hatası, 0 uyarı

**Henüz Yapılmamış / Test Edilmemiş:**
- Runtime test: Henüz Visual Studio'da debug ile test edilmedi
- SIP bağlantısı: Gerçek bir SIP sunucu (Asterisk/FreeSWITCH/cloud) ile test gerekiyor — arkadaştan yardım istenecek
- Windows ve Mobil uygulamalar: Placeholder UI, Faz 5-6'da gelecek
- İleri özellikler: CRM, çağrı kaydı, frontend i18n — Faz 7
- Dış entegrasyon: API Gateway, webhook — Faz 8

**Sonraki Öncelikli Adımlar:**
1. Visual Studio'da debug ile runtime test (login, CRUD, SignalR)
2. Gerçek SIP sunucuya bağlanma testi (arkadaştan destek)
3. Runtime'da çıkan hataları düzeltme
4. Faz 5 veya 7'ye geçiş (kullanıcı kararına göre)

### 2026-02-10 - Görüntülü Görüşme Araştırması (Video Call)

**Motivasyon:** Finans kurumları görüntülü görüşme talep ediyor (kimlik doğrulama, müşteri hizmetleri, uzaktan danışmanlık).

#### Mevcut Altyapıyla Uyum
- **SIP.js 0.21.2 video destekliyor.** Mevcut `sipClient.js`'de `video: false` → `video: true` yapılması yeterli.
- Ek olarak `<video>` HTML elementleri ve `_setupRemoteMedia` güncellemesi gerekiyor.
- Yani mevcut sesli arama altyapısı üzerine video eklenebilir, sıfırdan yazmaya gerek yok.

#### Codec Seçimi
- **H.264 zorunlu** — Tüm platformlarda (özellikle iOS/Safari) çalışan tek codec.
- VP8 ikinci seçim (açık kaynak, lisans ücretsiz).
- AV1 gelecek vaat ediyor ama şu an için erken (CPU gereksinimleri çok yüksek).

#### Bant Genişliği
| Kalite | Çözünürlük | Bitrate | Call Center Önerisi |
|--------|-----------|---------|---------------------|
| Düşük | 320x180 | 100-200 Kbps | Saha personeli (4G) |
| Orta | 640x360 | 400-700 Kbps | Standart görüşme |
| Yüksek | 1280x720 | 1-1.5 Mbps | Finans/kimlik doğrulama |

#### Platform Durumu
| Platform | Video Desteği | Güvenilirlik | Not |
|----------|--------------|--------------|-----|
| Web (Blazor WASM) | SIP.js + WebRTC | Yüksek | Mevcut yapı üzerine eklenir |
| Windows (WPF Hybrid) | WebView2 + WebRTC | Yüksek | Kamera izni ayarı gerekli |
| Android (MAUI) | Sorunlu | Orta | WebRTC host sorunu, workaround lazım |
| iOS (MAUI) | H.264 zorunlu | Orta-Yüksek | CallKit entegrasyonu gerekebilir |

#### Ekran Paylaşımı
- `getDisplayMedia()` API ile tamamen client-side yapılabilir, sunucu değişikliği gerektirmez.
- Kullanım: Agent → müşteriye ekran gösterme (teknik destek), müşteri → agent'a sorun gösterme.

#### Grup/Konferans Video
- **Basit (supervisor dinleme):** PBX conference bridge yeterli
- **3-5 kişi:** LiveKit öneriliyor (.NET SDK var, kurulumu basit, simulcast + kayıt yerleşik)
- **Büyük konferans:** mediasoup veya Janus

#### Video Kayıt
- **Client-side:** MediaRecorder API (basit ama güvenilir değil)
- **Server-side:** Janus/LiveKit ile medya sunucu tarafında kayıt (finans için zorunlu — yasal uyum)

#### SIP Sunucu Gereksinimleri
- **Asterisk:** Passthrough modu (transcoding yok, iki uç aynı codec kullanmalı). Codec listesine h264,vp8 eklenmeli.
- **FreeSWITCH:** MCU modu ile video konferans da destekler. mod_av gerekli.
- **Cloud:** Telnyx (en kolay entegrasyon, SIP.js tabanlı), Twilio Video (ayrı ürün), Vonage Video API

#### Yapılacaklar (Video Entegrasyonu İçin)
1. `sipClient.js` — video constraint ekleme, `_setupRemoteMedia` güncelleme
2. `index.html` — `<video id="remoteVideo">` + `<video id="localVideo" muted>` elementleri
3. `VideoPanel.razor` — Uzak video (büyük) + lokal video (küçük, sağ altta)
4. Ekran paylaşımı — `getDisplayMedia()` wrapper + UI butonu
5. WPF Hybrid — WebView2 `PermissionRequested` event handler (kamera izni)
6. SIP sunucu — Video codec yapılandırması
7. Video kayıt — Finans için server-side kayıt (LiveKit/Janus)

#### En Büyük Risk
MAUI Blazor Hybrid'de (Android) WebRTC video tutarsız. Mobilde video için native katman veya WebRTCme framework gerekebilir.

### 2026-02-10 - Görev 4.9: Müşteri Detay Sayfası (Tabbed Yönetim)

**Amaç:** Admin firma oluşturduktan sonra tek yerden yönetim. Modül atama, personel oluşturma, kuyruk/SIP görüntüleme hepsi tek sayfada.

**CustomerDetail.razor (/admin/customers/{id}):**
- 5 sekme: Genel, Modüller, Personel, Kuyruklar, SIP Hesapları
- **Genel**: Firma bilgileri inline EditForm (CustomerUpdateDto ile PUT)
- **Modüller**: 14 kart, toggle switch ile aç/kapat (POST/DELETE api/customers/{id}/modules)
- **Personel**: Portal'daki PersonnelForm ve PersonnelPermissionForm bileşenlerini yeniden kullanıyor (CustomerIdParam ile)
- **Kuyruklar**: Read-only tablo, /admin/queues'a link
- **SIP**: Read-only tablo, /admin/sip'e link
- Lazy tab loading: Sekme ilk açıldığında veri yükleniyor

**NavMenu Güncelleme:**
- Müşteriler artık ayrı bir menü grubu (grup index 4)
- Yönetim grubu: Kullanıcılar, Kuyruk Yapılandırması, SIP, Dil, Sistem (Müşteriler çıkarıldı)
- GroupRoutes ayrımı: `admin/customers*` → grup 4, diğer `admin/*` → grup 3

**Customers.razor Güncelleme:**
- "Yönet" butonu eklendi (bi-sliders2 ikonu, /admin/customers/{id}'ye yönlendirir)
- 4 buton sırası: Yönet, Portal, Düzenle, Sil

**Yeni Dosyalar (2):** CustomerDetail.razor + CSS
**Düzenlenen Dosyalar (2):** Customers.razor, NavMenu.razor
**Build: 0 hata, 0 uyarı**

#### Mobil Uygulama Teknoloji Kararı (ÖNEMLİ)

**Karar:** Mobil uygulama MAUI yerine **React Native** ile yazılacak.

**Gerekçe:**
- MAUI Blazor Hybrid'de WebRTC/video Android'de sorunlu (kamera erişimi, host güvenlik sorunu)
- React Native ve Flutter'da WebRTC native çalışır — WebView kısıtlaması yok
- Backend REST API + SignalR olduğu için mobil client herhangi bir teknolojiyle yazılabilir
- Telnyx'in React Native SDK'sı mevcut (`newCall({ video: true })` ile video arama)

**Yeni platform stratejisi:**
| Platform | Teknoloji | Durum |
|----------|-----------|-------|
| Web | Blazor WebAssembly | Mevcut (Faz 2 tamamlandı) |
| Windows | WPF Blazor Hybrid (MAUI değil) | Faz 5'te yapılacak |
| **Mobil** | **React Native** | **Ayrı proje olarak yazılacak** |

**Mobil uygulamanın backend bağlantısı:**
- REST API → Aynı endpoint'ler (login, CRUD, portal)
- SignalR → Aynı hub (gerçek zamanlı bildirimler)
- SIP/WebRTC → Native kütüphane (react-native-webrtc veya flutter-webrtc)
- Push notification → Yeni endpoint gerekecek (device token kayıt)
- Video → Native WebRTC ile sorunsuz

**Avantajlar:**
- Mobili ayrı bir geliştirici paralel yazabilir (API hazır)
- WebRTC/video/kamera native çalışır
- CallKit (iOS) ve ConnectionService (Android) entegrasyonu kolay
- Tek codebase ile Android + iOS

**MAUI projesi kaldırılmayacak** — Windows masaüstü için hâlâ kullanılabilir. Sadece mobil taraf ayrı yazılacak.

---

## Görev 4.10 — Müşteri Organizasyon Hiyerarşisi (2026-02-10)

Müşteri altında organizasyon ağacı, kullanıcı tipi hiyerarşisi ve personel raporlama zinciri eklendi.

### Mimari Kararlar

**Self-Referencing Ağaç Yapısı:**
- `CustomerOrganizationUnit.ParentId` → sınırsız derinlik
- `CustomerPersonnel.ReportsToPersonnelId` → amir-ast zinciri
- İkisinde de BFS tabanlı cycle detection (oluşturma/güncelleme sırasında)
- Delete: Çocuğu olan birim silinemez (Restrict), personnel FK'ları SetNull

**TypeItem ile Birim Tipi:**
- OrganizationUnitTypes: Region(1), Branch(2), Department(3), Unit(4), Team(5)
- Her tipin ikonu ve CSS sınıfı var → ağaç görünümünde gösterilir

**Kullanıcı Tipi Hiyerarşisi:**
- `CustomerUserType.Level` (int, 1 = en yüksek) → seviye bazlı sıralama
- `CanManageSubordinates` → alt personeli yönetebilir flag'i
- `CanApprove` → Faz 2 placeholder (sadece alan, iş mantığı yok)

**Unique Constraint:**
- `(CustomerId, Name, ParentId)` → aynı isimde farklı üst birimlerde olabilir
- PostgreSQL'de NULL != NULL → kök birimler arasında da aynı isim olabilir

### Dosya Listesi

**Yeni Dosyalar (~15):**
| Dosya | Açıklama |
|-------|----------|
| Entities/CustomerOrganizationUnit.cs | Self-ref ağaç entity |
| DTOs/OrganizationDtos.cs | List, Tree, Detail, Create, Update DTO'lar |
| Services/Interfaces/IOrganizationService.cs | 7 metod interface |
| Services/OrganizationService.cs | CRUD + ağaç build + cycle detection |
| Controllers/OrganizationsController.cs | 7 endpoint, Admin/Supervisor auth |
| Components/OrgTreeNode.razor + .css | Recursive ağaç component |
| Pages/Admin/Organizations.razor | Ağaç görünümü + detay paneli |
| Pages/Admin/OrganizationForm.razor | Create/Edit modal |
| Pages/Admin/UserTypes.razor | Admin kullanıcı tipleri sayfası |
| Pages/Admin/AdminUserTypeForm.razor | Create/Edit modal |
| Pages/Admin/Personnel.razor | Admin personel sayfası |
| Pages/Admin/AdminPersonnelForm.razor | Create/Edit modal (OrgUnit/ReportsTo) |
| Migration: AddOrganizationHierarchy | Tablo + FK + index |

**Düzenlenen Dosyalar (~14):**
| Dosya | Değişiklik |
|-------|-----------|
| CustomerUserType.cs | +Level, +CanManageSubordinates, +CanApprove |
| CustomerPersonnel.cs | +OrganizationUnitId, +ReportsToPersonnelId, +Subordinates nav |
| Queue.cs | +OrganizationUnitId |
| SipAccount.cs | +OrganizationUnitId |
| Customer.cs | +OrganizationUnits nav |
| TypeDefinitions.cs | +OrganizationUnitTypes class |
| AppDbContext.cs | DbSet + FK configs |
| PortalDtos.cs | UserType/Personnel DTO'lara yeni alanlar |
| AdminDtos.cs | Queue/SipAccount DTO'lara OrgUnitId/Name |
| ServiceFactory.cs | +CreateOrganizationService() |
| Program.cs | +IOrganizationService DI |
| PortalService.cs | UserType/Personnel CRUD güncelleme |
| QueueService.cs | OrgUnitId/Name mapping |
| SipAccountService.cs | OrgUnitId/Name mapping |
| NavMenu.razor | +3 link (Organizasyonlar, Kullanıcı Tipleri, Personel) |
| CustomerDetail.razor | +Organizasyon sekmesi (6. tab) |

### Faz 2'ye Bırakılanlar
- Level enforcement (runtime seviye kontrolü)
- CanApprove mekanizması (onay iş akışı)
- Toplu birim silme (cascading soft delete)

### 2026-02-10 - Görev 4.11: SearchableSelect (Aranabilir Combobox) Componenti

**Amaç:** 12+ dropdown'u aranabilir hale getirmek. Firma, personel, organizasyon birimi gibi potansiyel olarak çok kayıtlı listeler düz `<select>` ile kullanışsız.

**SearchableSelect Component (2 yeni dosya):**
- `Components/SearchableSelect.razor` + CSS
- Parametreler: Items (SearchSelectItem listesi), Value, ValueChanged, Placeholder, SearchPlaceholder, AllowClear, Disabled
- SearchSelectItem record: Value, Text, Subtitle (opsiyonel)
- Overlay yaklaşımı ile click-outside kapatma (JS interop yok)
- Case-insensitive arama (Text + Subtitle üzerinde Contains)
- FocusAsync ile açıldığında otomatik arama inputuna odaklanma

**Dönüştürülen dropdown'lar (10 dosya, 12 select):**
- Sayfa filtreleri (5): Personnel, UserTypes, Organizations, Reports/Agents, Reports/Calls → firma seçici
- Form modalleri (5): AdminPersonnelForm (3: UserType/OrgUnit/ReportsTo), OrganizationForm (2: ÜstBirim/Yönetici), QueueForm (1: Firma), SipAccountForm (1: Firma), Portal/PersonnelForm (1: UserType)

**Dönüştürülmeyen küçük enum'lar:** Rol (4), Birim Tipi (5), Transport (4), Yön (3), Durum (5)

**Build: 0 hata, 0 uyarı (tüm solution)**

### 2026-02-10 - SIP/VoIP Sağlayıcı Demo Hesap Araştırması

**Amaç:** Faz 3'teki SIP.js entegrasyonunu gerçek bir SIP sunucuya bağlayarak runtime'da test etmek.

**SIP.js ile En Uyumlu Seçenekler (Öncelik Sırasıyla):**
1. **OnSIP** — SIP.js'yi geliştiren firma! Ücretsiz developer hesabı, kredi kartı gerektirmez. **İlk test için en mantıklı.**
2. **SignalWire** — FreeSWITCH'in yaratıcıları. Standart SIP over WebSocket, SIP.js doğrudan çalışır. $5 depozit.
3. **Telnyx** — SDK zaten SIP.js tabanlı. $5 depozit + iş e-postası gerekli.
4. **Asterisk (self-hosted)** — $5-10/ay VPS, tam kontrol, en yaygın.

**Ücretsiz Test Sunucuları:**
- OnSIP: Ücretsiz SIP hesabı, SIP.js birebir uyumlu
- SIP2SIP.info: Ücretsiz, echo test (`echo@conference.sip2sip.info`)
- sip5060.net: Test numaraları (echo, DTMF), hesap gerektirmez

**Önerilmeyenler:** Bandwidth (WebRTC API kaldırıldı), VoIP.ms (WebSocket yok), 3CX Free (kapalı SIP stack, SIP.js çalışmaz)

**Detaylı analiz:** `yol_haritasi.xml` → `<Arastirma konu="SIP/VoIP Saglayici Demo Hesaplari">`

### 2026-02-11 - Düzenleyici Uyum Araştırması (BDDK, KVKK, BTK, SPK, ISO 27001)

**Amaç:** Call center projemizin finans, sigorta ve kurumsal müşterilere satılabilmesi için Türkiye'deki bilgi güvenliği düzenlemelerine uyum gereksinimlerinin tespiti.

#### 1. BDDK — Bankaların Bilgi Sistemleri ve Elektronik Bankacılık Hizmetleri Hakkında Yönetmelik
**Kaynak Yönetmelik:** 15 Mart 2020 tarihli Resmi Gazete (Yürürlük: 1 Temmuz 2020)

**Şifreleme Gereksinimleri:**
- **Data in Transit:** Tüm iletişimde güçlü şifreleme zorunlu (TLS 1.2 minimum, TLS 1.3 önerilir)
- **Data at Rest:** AES-256 veya RSA-2048 ile disk/dosya/veritabanı şifreleme
- Şifreleme gizli anahtarları ile doğrulama kodlarının imzalanması, inkar edilemezlik sağlanması
- Finansal işlemlerde tek kullanımlık doğrulama kodları (tutar+alıcı bilgisine özel)

**Kimlik Doğrulama (MFA):**
- En az 2 bağımsız faktör (bilgi + sahiplik veya biyometrik)
- BDDK 2023/1 sayılı Genelge: Elektronik bankacılıkta kimlik doğrulama ve işlem güvenliği kriterleri
- Riskli işlemler için çok faktörlü kimlik doğrulama zorunluluğu (2025 itibarıyla)

**Erişim Kontrolü:**
- Rol tabanlı erişim kontrolü (RBAC) zorunlu
- Hassas verilere erişimde yetki matrisi
- Tüm sorgulamalar kayıt altında
- Yurt dışına veri aktarımı sınırlandırması

**Veri Sınıflandırma:**
- Varlık envanteri ve veri envanteri hazırlama zorunluluğu
- Güvenlik sınıfları ve erişim haklarının belirlenmesi
- Varlık sınıflandırma kılavuzu hazırlama zorunluluğu
- "Hassas veri" tanımı: Üçüncü taraflara açıklanması zarar verebilecek her türlü veri

**Felaket Kurtarma / İş Sürekliliği:**
- Birincil ve ikincil sistemler yurt içinde konumlandırılmalı
- Birincil sistemler tamamen devre dışı kalsa bile en geç 24 saat içinde faaliyet sürdürülebilmeli
- BS Süreklilik Komitesi kurulması zorunlu (İK, hukuk, iş birimleri, BS güvenlik temsilcileri)
- BS Strateji Komitesi, BS Yönlendirme Komitesi, Bilgi Güvenliği Komitesi zorunlu

**Penetrasyon Testi:**
- Yılda en az 1 kez BDDK onaylı sızma testi zorunlu
- İki aşama: temel + detaylı sızma testleri
- Kapsam: İletişim altyapısı, DNS, etki alanı, e-posta, veritabanları, web/mobil uygulamalar, kablosuz ağlar, DDoS, sosyal mühendislik
- Raporlar en geç 1 ay içinde BADES'e (Bağımsız Denetim Takip Sistemi) yüklenecek

**ISO 27001 Zorunluluğu:**
- BDDK ISO/IEC 27001 sertifikasyonunu zorunlu tutuyor
- Bağımsız denetime tabi kuruluşlar tarafından denetlenip sertifikalandırılması gerekiyor
- COBIT standartlarına uyum da zorunlu
- Yılda en az 90 saat zorunlu personel eğitimi

#### 2. KVKK — Kişisel Verilerin Korunması Kanunu (6698 sayılı)

**Teknik Tedbirler:**
- Şifreleme (mümkün olan her yerde ayrı ayrı anahtarlar)
- Disk, dosya ve veritabanı şifreleme
- Yetki matrisi oluşturma
- Loglama (tüm erişim kayıtları)
- SIEM ile log analizi
- Politika ve eğitim

**Log/Audit Trail:**
- Silme, yok etme, anonimleştirme işlemleri kayıt altına alınmalı
- Bu kayıtlar en az 3 yıl saklanmalı
- Erişim logları tutulmalı ve bütünlüğü korunmalı

**Veri Saklama ve İmha:**
- Periyodik imha süresi en fazla 6 ay
- Kişisel Veri Saklama ve İmha Politikası zorunlu
- Veri sınıflandırma, saklama süreleri belirleme ve periyodik imha planı
- Genel zamanaşımı (TTK): 10 yıl (ses kayıtları dahil)

**Çağrı Merkezi Özeli:**
- Ses kaydı alınması için açık rıza veya meşru hukuki dayanak gerekli
- Aydınlatma metni zorunlu ("Güvenliğiniz için konuşmanız kayda alınıyor")
- Kayıt transkriptleri veri sahibine maskelenerek paylaşılabilir
- Çağrı merkezi verileri: müşteri ilişki yönetimi, şikayet takibi, iletişim, hukuki kayıt, müşteri güvenliği amaçları

#### 3. BTK — Bilgi Teknolojileri ve İletişim Kurumu

**Temel Yönetmelik:** Elektronik Haberleşme Sektöründe Şebeke ve Bilgi Güvenliği Yönetmeliği (13/07/2014, RG No: 29059)

**VoIP/SIP Düzenlemeleri:**
- VoIP hizmeti sunmak için BTK'ya bildirim/yetkilendirme zorunlu
- 5809 sayılı Elektronik Haberleşme Kanunu kapsamında
- Numara, frekans gibi kaynak tahsisi gerekiyorsa kullanım hakkı alınması zorunlu
- Sektörel Siber Olaylara Müdahale Ekibi (Sektörel SOME) kapsamında bildirim yükümlülüğü

**Log Tutma Yükümlülükleri:**
- Kişisel verilere ve ilişkili sistemlere yapılan erişim kayıtları: 2 yıl saklama zorunlu
- Erişim yetkili personelin tüm işlemleri detaylı kayıt altında
- Bilgi güvenliği ihlal olayları kayıt altına alınıp değerlendirilecek
- **Yaptırım:** Uymayanlar için net satışların %3'üne kadar idari para cezası

**Güvenlik Gereksinimleri:**
- Şebeke ve bilgi güvenliğinin sağlanması için BGYS (Bilgi Güvenliği Yönetim Sistemi) kurulması
- İşletmecilerin uyacakları usul ve esaslar yönetmelikle belirlenmiş

#### 4. SPK — Sermaye Piyasası Kurulu

**Çağrı Kaydı Saklama Süreleri:**
- Sözlü emir ses kayıtları: 3 yıl (emir tarihinden itibaren, önceki 2 yıldan uzatıldı)
- Tüm emir formları, elektronik emirler, faks kayıtları: 10 yıl (önceki 5 yıldan uzatıldı)
- Yatırım kuruluşları müşteri emirlerine ilişkin telefon kayıtlarını düzenli tutmak ve talep halinde SPK'ya sunmakla yükümlü

**Görüntülü Görüşme Gereksinimleri (Uzaktan Kimlik Tespiti):**
- Gerçek zamanlı ve kesintisiz yapılmalı
- Görsel-işitsel iletişimin bütünlüğü ve gizliliği yeterli seviyede olmalı
- Uçtan uca güvenli iletişim (end-to-end encryption)
- Görüntü ve ses kalitesi tüm görüşme boyunca yeterli seviyede (kimlik tespitinde kısıtlama olmamalı)
- Başvuru elektronik formla alınmalı, risk değerlendirmesi yapılmalı

#### 5. SIP/VoIP Güvenlik Gereksinimleri (Endüstri Standartları)

**SIP Sinyalleşme Güvenliği:**
- TLS zorunlu (port 5061 şifreli, port 5060 şifresiz)
- SIP başlıkları arayan/aranan numaraları ve kimlik doğrulama verileri içerir → TLS ile korunmalı
- WSS (WebSocket Secure) zorunlu (tarayıcı tabanlı SIP.js bağlantıları için)

**Medya Şifreleme:**
- SRTP (Secure Real-time Transport Protocol) zorunlu
- RTP ses/video akışlarını şifreler
- SRTP'nin etkin kullanımı için TLS ön koşul
- Üçüncü taraflar ses akışını deşifre edemez, değiştiremez, bozamaz

**Call Center İçin Best Practice:**
- Uçtan uca şifreleme: SRTP (medya) + TLS (sinyalleşme)
- SIP trunk güvenliği: IP kısıtlama, güçlü kimlik doğrulama
- SRTP + TLS birlikte kullanılmalı (biri diğerinin yerine geçmez)

#### 6. Genel Saklama Süreleri Özeti

| Veri Türü | Süre | Dayanak |
|-----------|------|---------|
| Ses kayıtları (genel) | 10 yıl | TTK md. 82, Bankacılık Kanunu md. 42 |
| SPK sözlü emir kayıtları | 3 yıl | SPK Belge ve Kayıt Düzeni Tebliği |
| SPK emir formları/dokümanlar | 10 yıl | SPK Belge ve Kayıt Düzeni Tebliği |
| KVKK silme/imha kayıtları | 3 yıl | KVKK Yönetmeliği |
| BTK erişim logları | 2 yıl | Şebeke ve Bilgi Güvenliği Yönetmeliği |
| KVKK periyodik imha | Max 6 ay aralık | KVKK Yönetmeliği |

#### 7. Projemize Etkisi — Teknik Uyum Gereksinimleri

**Acil (MVP'de olmalı):**
1. **TLS + SRTP**: SIP bağlantılarında TLS sinyalleşme + SRTP medya şifreleme (mevcut SipAccount.UseSrtp + Transport=TLS/WSS zaten var)
2. **Audit Log**: Tüm kullanıcı işlemleri (login, CRUD, SIP, çağrı) için log tablosu ve kayıt mekanizması
3. **Çağrı Kaydı Saklama**: Ses kayıtları için şifreli depolama (AES-256), metadata + dosya yolu + TTL
4. **KVKK Aydınlatma**: Çağrı başlangıcında otomatik aydınlatma mesajı çalma desteği
5. **MFA**: Admin ve Supervisor kullanıcılar için iki faktörlü kimlik doğrulama (TOTP)
6. **Şifre Politikası**: Minimum uzunluk, karmaşıklık, süre aşımı, tekrar engelleme
7. **SipAccount.Password Şifreleme**: Düz metin → AES şifreleme (Data at Rest)

**Orta Vadeli (Faz 7-8):**
1. **Veri Sınıflandırma**: Veri envanteri ve sınıflandırma sistemi
2. **Periyodik İmha**: Süresi dolan kayıtların otomatik imha mekanizması
3. **RBAC Genişletme**: Daha granüler erişim kontrolü
4. **Penetrasyon Testi Raporlama**: Pentest sonuçları için dashboard
5. **İş Sürekliliği**: DR planı, yedekleme, failover
6. **Video Kayıt**: Server-side video kayıt (SPK uzaktan kimlik tespiti uyumu)
7. **IP Kısıtlama**: SIP trunk erişiminde IP whitelist

**Uzun Vadeli (Enterprise):**
1. ISO 27001 sertifikasyon desteği (kontrol listesi, kanıt toplama)
2. BDDK BADES entegrasyonu
3. COBIT uyum raporlama
4. Felaket kurtarma otomasyonu
5. SIEM entegrasyonu (log'ların dış sisteme aktarımı)

**Kaynaklar:**
- [BDDK Bilgi Sistemleri Düzenlemeleri](https://www.bddk.org.tr/Mevzuat/Liste/134)
- [BDDK Yönetmelik (Resmi Gazete)](https://www.resmigazete.gov.tr/eskiler/2020/03/20200315-10.htm)
- [BDDK Kimlik Doğrulama Genelgesi 2023/1](https://www.bddk.org.tr/Mevzuat/DokumanGetir/1171)
- [KVKK Veri Güvenliği Rehberi](https://www.kvkk.gov.tr/yayinlar/veri_guvenligi_rehberi.pdf)
- [KVKK Saklama ve İmha Politikası](https://www.kvkk.gov.tr/Icerik/5386/KVKK-KISISEL-VERI-SAKLAMA-ve-IMHA-POLITIKASI)
- [BTK Şebeke ve Bilgi Güvenliği Yönetmeliği](https://www.btk.gov.tr/sebeke-ve-bilgi-guvenligi-mevzuat)
- [SPK Belge ve Kayıt Düzeni](https://www.procompliance.net/spknin-belge-ve-kayit-duzeni-tebligi-ne-getirdi/)
- [SPK Uzaktan Kimlik Tespiti](https://legal.com.tr/blog/genel/araci-kurumlar-ve-portfoy-yonetim-sirketlerince-kullanilacak-uzaktan-kimlik-tespiti-yontemleri/)
- [BDDK Penetrasyon Testi](https://www.nesilteknoloji.com/bddk-ile-uyumlu-sizma-testi-nedir/)
- [Google Cloud BRSA Uyumu](https://cloud.google.com/security/compliance/brsa-turkey)
- [Turkey BRSA Banking Regulation](https://cloud.google.com/security/compliance/brsa_banking_outsourcing_regulations_workspace_mapping)
- [Çağrı Merkezi Yasal Sorumluluklar](https://bluecom.com.tr/cagri-merkezi-yasal-sorumluluklari-ve-mevzuat/)

---

### 2026-02-11 - Faz 5: Windows Softphone Uygulaması (Devam Ediyor)

**Görev 5.1 (TAMAMLANDI): Altyapı, DI, Auth**
- SecureStorage (dosya tabanlı) → localStorage yerine
- WindowsAuthService, WindowsAuthStateProvider, WindowsAuthHeaderHandler
- MainWindow.xaml.cs tam DI konfigürasyonu
- NuGet: SIPSorcery 6.2.4, NAudio 2.2.1, SignalR.Client, JWT, Authorization
- **SIPSorceryMedia.Windows .NET 10 ile UYUMSUZ** — NAudio direkt kullanıldı

**Görev 5.2 (TAMAMLANDI): Login + Layout + Sayfalar**
- Login, Dashboard, MainLayout, NavMenu, LoginLayout — Web'den adapte edildi
- RedirectToLogin, ToastNotification, ToastService, WindowsPermissionService

**Görev 5.3 (TAMAMLANDI): SignalR**
- WindowsHubService — Web HubService'ten adapte
- Build: 0 hata, 0 uyarı (5.1+5.2+5.3 sonrası)

**Görev 5.4 (DEVAM EDİYOR): SIPSorcery Native SIP**
- ISipService interface + NativeSipService.cs oluşturuldu
- **8 BUILD HATASI VAR** — SIPSorcery 6.2.4 API yanlış kullanılmış

**Kritik Bulgular:**
1. `SIPSorceryMedia.Windows` sadece net6/net8 destekliyor → NAudio ile ses I/O yapılmalı
2. `VoIPMediaSession` constructor'ı `MediaEndPoints` tipini almıyor (6.2.4'te yok)
3. `AudioSourcesEnum.CaptureDevice` yok — sadece test kaynakları var (WhiteNoise, Silence, Music)
4. `AudioCodecsEnum` tipi SIPSorcery 6.2.4'te mevcut değil
5. `SIPUserAgent.Answer()` → 3 parametre alıyor: `(SIPServerUserAgent, IMediaSession, IPAddress)`
6. Gelen arama akışı: `AcceptCall(SIPRequest)` ile SIPServerUserAgent oluşturulmalı

**Görev 5.4 (TAMAMLANDI): SIPSorcery Native SIP — DÜZELTME**
- SIPSorceryMedia.Windows 8.0.14 eklendi (TFM: net10.0-windows10.0.17763 ile uyumlu)
- WindowsAudioEndPoint + AudioEncoder ile gerçek mikrofon/hoparlör
- Answer(SIPServerUserAgent, IMediaSession) doğru imza kullanıldı
- AcceptCall(sipRequest) ile gelen arama akışı düzeltildi
- TakeOffHold() void dönüyor → async kaldırıldı
- AudioCodecsEnum → SIPSorceryMedia.Abstractions namespace
- 8 build hatası tamamen düzeltildi

**Görev 5.5 (TAMAMLANDI): Agent Sayfaları**
- Dialer.razor + CSS: Web'den adapte, ISipService.InitializeAsync(SipConnectionInfoDto) kullanır
- Calls/Active.razor: Aktif aramalar + kuyrukta bekleyenler
- Calls/History.razor: Arama geçmişi + filtreler
- TransferDialog.razor: Blind transfer modal
- IncomingCallNotification.razor: SignalR + SIP gelen arama popup
- AudioSettings.razor: NAudio cihaz listeleme (DeviceIndex tabanlı, DeviceId değil)
- MainLayout'a AudioSettings + IncomingCallNotification eklendi

**Görev 5.6 (TAMAMLANDI): System Tray + Bildirimler**
- Hardcodet.NotifyIcon.Wpf 1.1.0: System tray ikonu
- Microsoft.Toolkit.Uwp.Notifications 7.1.3: Windows toast
- SystemTrayService.cs: Tray, close-to-tray, double-click restore, gelen arama toast
- MainWindow.xaml.cs: SystemTrayService DI + Loaded event

**Görev 5.7 (TAMAMLANDI): Dağıtım**
- Properties/PublishProfiles/win-x64.pubxml: Self-contained, single-file, ReadyToRun

**FAZ 5 TAMAMLANDI — 0 hata, 0 uyarı (tüm 6 proje)**

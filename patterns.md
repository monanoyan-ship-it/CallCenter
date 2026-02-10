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

**Mimari Karar: Factory + Service Pattern (Yeni Kod İçin)**
- Faz 1-3'teki controller'lar mevcut yapıda kalır (doğrudan DbContext)
- Faz 4'ten itibaren: Controller → ServiceFactory → IPortalService → AppDbContext
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

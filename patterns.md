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
- **KURAL**: Standart C# enum kullanılmayacak, TypeItem pattern kullanılacak. [Flags] CustomerPermission hariç.
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

## Dış Entegrasyon Vizyonu (Faz 7)

Bu proje izole bir uygulama değil. Dış dünya ile iki yönlü bağlantı kuracak:
- **Outbound** (biz → dış sistemler): CRM, ERP, Helpdesk sistemlerine bağlanma (Salesforce, SAP, Zendesk vb.)
- **Inbound** (dış sistemler → biz): REST API, Webhook, SignalR ile dış sistemlerin bize erişimi
- **Mevcut uyum**: Halihazırda kullanılan takip uygulamalarından veri göçü, paralel çalışma desteği
- **Teknik**: Adapter/Connector pattern, API Key/OAuth2 güvenlik, Swagger/OpenAPI dokümantasyon, SDK

# Call Center Projesi - Geliştirme Desenleri ve Kararlar

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

# Call Center Projesi - Ajan Talimatlari

Bu dosya Codex/Claude ajanlari icin repo kok rehberidir. Her oturumda once ClaudeManager okunur; bu dosya ise proje haritasi, calisma kurallari ve kritik tuzaklari hizli hatirlatir.

## KESIN EMIRLER

### 1. ClaudeManager ZORUNLU
Her oturumun ILK isi ClaudeManager rehberini okumaktir. Rehber okunmadan KOD YAZILMAZ.

**project_id: 15** - Tum endpoint'lerde bu ID kullanilir. `?cwd=` KULLANMA.

```
curl -s http://127.0.0.1:41847/api/projects/15/patterns
curl -s http://127.0.0.1:41847/api/projects/15/notes
curl -s http://127.0.0.1:41847/api/projects/15/journal
curl -s http://127.0.0.1:41847/api/projects/15/roadmap/summary
```

ClaudeManager okunamazsa kullaniciyi bilgilendir ve onay almadan devam etme.

### 2. Oturum Koordinasyonu
Birden fazla ajan oturumu ayni anda calisabilir. Cakismayi onlemek icin:
- Once `git status --short` ile kirli worktree gor.
- Dokunacagin dosyalari dar kapsamda tut; ilgisiz degisikliklere dokunma.
- Ayni dosyaya paralel oturum dokunuyorsa kullanicidan yonlendirme al.
- Kalici planlar ve gorev durumlari ClaudeManager roadmap endpoint'lerine yazilir.

### 3. Workspace Siniri
- Kullanici acikca hedef klasor soylemedikce acilan repo kokunun disina cikma.
- PC genelinde arama/kurcalama yapma; sadece bu repo ve kullanicinin acikca verdigi hedef klasorler uzerinde calis.

### 4. Git Kurallari
- Kullanici acikca "commit et" demeden commit yapma.
- Kullanici acikca "push et" demeden push yapma. Commit onayi push onayi degildir.
- Kullanici degisikliklerini asla revert etme.
- Kirli worktree varsa once `git status --short` ile gor, ilgisiz degisikliklere dokunma.

### 5. Calistirma Kurallari
- `dotnet run`, `dotnet watch` ve benzeri debug/run komutlari yasak. Debug Visual Studio ile yapilir.
- `dotnet build` serbest.
- `dotnet test` serbest.
- Basit dosya aramalarinda once `rg` kullan.

### 6. ClaudeManager'a Yazma Zorunlulugu
- Yeni kalici kural/hata/tercih ogrenilirse Pattern olarak kaydet.
- Yeni hesap/API key/sifre/config olusturulursa Notes'a yaz.
- Gunluk bilgi, deploy, domain, kredi, basvuru gibi bilgiler Journal'a yaz.
- Duzeltilecek hata, eksik, UX kusuru veya teknik borc Notes'a degil roadmap task'ina yazilir ve tamamlandikca kapatilir.
- Gorev tamamlaninca ilgili roadmap task'ina risk/eksik raporu ekle.

## Proje Ozeti

CorpLynk CallCenter cok urunlu bir sistemdir. Ana hatlariyla tek backend API ve bu API'yi kullanan farkli uygulamalardan olusur.

### Solution
Ana solution: `CallCenter.slnx`

Projeler:
- `src/CallCenter.Api`: merkezi ASP.NET Core Web API + SignalR backend.
- `src/CallCenter.Data`: EF Core `AppDbContext`, PostgreSQL mapping ve migrations.
- `src/CallCenter.Shared`: entity, DTO, TypeItem tanimlari, auth helper, localization, ortak servisler.
- `src/CallCenter.Salon`: Salon MVC uygulamasi.
- `src/CallCenter.Management`: Platform yonetim MVC uygulamasi.
- `src/CallCenter.Web`: Call center Blazor WebAssembly frontend.
- `src/CallCenter.Windows`: Windows softphone / agent uygulamasi.
- `src/CallCenter.PbxService`: merkezi PBX worker servisi.
- `src/CallCenter.Crm`: CRM MVC uygulamasi.
- `src/CallCenter.Landing`: corplynk.com landing/public site.
- `tests/CallCenter.Tests`: xUnit test projesi.

## Uygulama Mimarisi

### API
Dosya: `src/CallCenter.Api/Program.cs`

API sistemin merkezidir:
- PostgreSQL + EF Core `AppDbContext`.
- JWT authentication.
- SignalR hub: `CallCenterHub`.
- API key middleware: `/api/integration/v1/*` icin `X-Api-Key`.
- OpenAPI development/AUTO_MIGRATE ortamlarinda acilir.
- Migration sadece Development veya `AUTO_MIGRATE=true` iken uygulanir.
- Startup'ta salon default data ve eksik modul seed kontrolu yapilir.

DI yapisi:
- `DependencyInjection/InfrastructureRegistration.cs`
- `DependencyInjection/EntityServiceRegistration.cs`
- `DependencyInjection/FactoryRegistration.cs`

Ana pattern:
`Controller -> Factory -> EntityService/IUnitOfWork -> AppDbContext`

Controller icinde is mantigi yazma. Controller sadece route, auth, claim okuma ve HTTP response donusumu yapmali.

### Data
Dosya: `src/CallCenter.Data/AppDbContext.cs`

Tum ana tablolar tek DbContext'tedir:
- Call center: User, Customer, CallRecord, Queue, SipAccount, SipLine.
- Salon: SlnClient, SlnAppointment, SlnInvoice, SlnCash, SlnBranch, SlnMembership, vb.
- CRM: CrmContact, Ticket, Deal, Activity, Quality.
- Billing/subscription/payment: SubscriptionPlan, CustomerSubscription, PaymentTransaction, PlatformPaymentConfig.
- KVKK, audit, integration, webhook, translation tablolari.

Kritik veri model kurallari:
- Primary key: `int Id`, auto-increment.
- Disari acik entity'lerde `Guid Uid` kullanilir; int ID URL/link olarak expose edilmez.
- Foreign key'ler `int`.
- Enum yerine `TypeItem` pattern kullanilir.

### Shared
Ortak katmandir:
- `Entities`: EF entity siniflari.
- `DTOs`: API/MVC/JS arasi tasinan modeller.
- `Enums`: `TypeItem` tanimlari.
- `Auth`: MVC uygulamalarinda JWT cookie parse helper.
- `Localization`: server-side translation cache ve `T()`/tag helper altyapisi.

Yeni TypeItem eklerken:
- `All`, `GetById`, `GetBySystemName` varsa guncelle.
- Inner `Ids` class icine const int ekle.
- Modul/feature ise seed, role permission ve UI menu akisini kontrol et.

### Salon
Dosya: `src/CallCenter.Salon/Program.cs`

Salon bir MVC uygulamasidir. DB'ye direkt gitmez; API'ye `SalonApi` HttpClient ile baglanir.

Onemli dosyalar:
- `Controllers/SlnBaseController.cs`: auth, rol, modul ve abonelik kontrolu.
- `Controllers/ProxyController.cs`: loginli `/proxy/{path}` isteklerini API'ye Bearer token ile iletir.
- `Controllers/PublicProxyController.cs`: public salon ve platform user proxy akislari.
- `Views/Shared/_Layout.cshtml`: sidebar, rol/modul menu filtresi, ortak JS/CSS.
- `wwwroot/js/*.js`: her sayfanin JS'i ayri dosyada.

Salon auth:
- Cookie adi: `CorpLynk.Salon.Auth`.
- Login API: `POST api/auth/login`.
- Register API: `POST api/auth/salon-register`.
- JWT icinden `CustomerModules`, `CustomerRoleId`, `BranchId` okunur.

Salon yetki katmanlari:
- Rol bazli: `SalonRolePermissions`.
- Modul bazli: `SalonModuleControllerMap` + `CustomerModules` claim.
- Abonelik bazli: `api/subscriptions/status`.

### Management
Dosya: `src/CallCenter.Management/Program.cs`

Management platform admin panelidir. DB'ye direkt gitmez; API'ye `ManagementApi` HttpClient ile baglanir.

Onemli dosyalar:
- `Controllers/MgmtBaseController.cs`: login ve Admin rol kontrolu.
- `Controllers/ProxyController.cs`: `/proxy/{path}` isteklerini API'ye Bearer token ile iletir.
- `Views/Shared/_Layout.cshtml`: admin sidebar.
- `wwwroot/js/*.js`: sayfa bazli JS dosyalari.

Management auth:
- Cookie adi: `CorpLynk.Mgmt.Auth`.
- Sadece `Role == Admin` panele girebilir.

Management kapsam:
- Musteriler, users, personnel, organizations.
- Modul fiyatlari, talepleri, envanteri, rol matrisi, abonelikler.
- Odeme ayarlari, email template, storage config, translations.
- KVKK, audit logs, billing reports.

## Frontend/MVC Kurallari

### JavaScript
- View icinde inline `<script>` yazma.
- Her sayfa icin `wwwroot/js/SayfaAdi.js` kullan.
- Native `alert()`, `confirm()`, `prompt()` yasak.
- Onay icin `confirmModal`, bildirim icin `toastr` kullan.
- AJAX global handler 401 icin login redirect yapabilir; 403 sayfanin kendi `.fail` handler'inda ele alinmali.
- KnockoutJS JSON property isimleri camelCase gelir. C# `UserName` -> JS `userName`.
- Knockout foreach icinde riskli property erisimlerinde `$data.prop` kullan.
- `createAutocomplete` kullanirken `data-bind="with: xxxAutocomplete"` kullanma; direkt `xxxAutocomplete.query` pattern'i kullan.

### Razor
- Razor `@page` directive ile cakisan degisken adi kullanma; `pageNum` gibi isim kullan.
- Inline lambda icindeki bos string literal Razor parser'i bozabilir; gerekiyorsa ayri method/helper kullan.
- `@bind-Value` ternary pattern kullanma; form model property tercih et.

### Localization
- Yeni `@T("key")`, `Localizer["key"]` veya translation key eklenirse TR ve EN cevirisi de eklenmeli.
- Management Dil Yonetimi veya XML import/export akisi kullanilir.
- Salon icin module filtreli ceviri cache kullanilir.

## Backend Kurallari

### Factory/Service
- Controller'da `AppDbContext` kullanma.
- Factory is mantigini tutar.
- EntityService tek entity uzerindeki query/add/update/delete islerini tutar.
- Kayit icin `IUnitOfWork.SaveChangesAsync()` kullan.
- Shared davranis gerekiyorsa once mevcut Factory/EntityService pattern'ini ara.

### DTO
- Partial update DTO'larinda bool alanlari `bool?` yap. Non-nullable bool default `false` ile mevcut ayarlari sifirlayabilir.
- Disari acik response'larda int ID yerine gerekiyorsa `Uid` tercih et.
- Tarih alanlarinda UTC uyumuna dikkat et. JS'den API'ye giden date/time degerlerinde UTC suffix karari onceki pattern'lere gore korunmali.

### Auth/Claims
- API JWT claim'leri CustomerId, CustomerModules, CustomerRoleId, BranchId gibi alanlarla Salon/Management akisini besler.
- Salon branch izolasyonu varsa `BranchId` claim query paramdan daha gucludur.
- Management controller'lari API tarafinda genelde `[Authorize(Roles = "Admin")]` ister.

### SignalR
- Gercek zamanli cagri, agent status, gateway health ve notification akislari SignalR ile yapilir.
- Gateway health icin REST endpoint yok; SignalR hub uzerinden gelir.

## Lokal ve Production URL'ler

Lokal development:
- API: `http://localhost:5041`
- API HTTPS: `https://localhost:7147`
- Salon: `http://localhost:5239`
- Management: `http://localhost:5280`

Production:
- API: `https://cc-api.corplynk.com`
- Salon: `https://sln.corplynk.com`
- Management: `https://mng.corplynk.com`
- Landing: `https://corplynk.com`

Credential, API key, DB sifresi ve deploy secret'larini bu dosyaya yazma. Bunlar ClaudeManager Notes'tadir.

## Deploy Kurallari

Deploy yapmadan once mutlaka ClaudeManager Notes oku:

```
curl -s http://127.0.0.1:41847/api/projects/15/notes
```

Onemli notlar:
- #52 API Deploy
- #114 Salon Deploy
- #137 Management Deploy
- #58 Windows App

Dockerfile kurali:
- Root `Dockerfile` her zaman API'ye ait olmali.
- Salon/CRM/Management deploy ederken root Dockerfile gecici degisir.
- Deploy bitince root Dockerfile hemen API'ye geri alinmali ve `head -3 Dockerfile` ile dogrulanmali.

Kritik deploy hatalari:
- API deploy oncesi root Dockerfile'in API oldugunu dogrula.
- `gcloud run deploy --set-env-vars` kullanma; mevcut env var'lari silebilir. Gerekirse `--update-env-vars` kullan.
- Degisiklik olan tum projeleri deploy et kuralini ClaudeManager'dan teyit et.
- Deploy oncesi silinen dosyalari `git diff` ile kontrol et; kullaniciya gorunen sayfa/endpoint silindiyse etki analizi yap.

## Bilinen Kritik Tuzaklar

- Google Drive / ses kaydi OAuth sistemine dokunma; yeni entegrasyonlar ayri credential ve token tablosu kullanmali.
- `CustomerUpdateDto` gibi partial update modellerinde non-nullable bool kullanma.
- Salon domain'i `sln.corplynk.com`; `salon.corplynk.com` degil.
- Management domain'i `mng.corplynk.com`; `mgmt` degil.
- Payment config ayni provider icin sandbox ve production kaydini birlikte tutabilir.
- Param test endpoint'i eski `test-dmz` degil; guncel bilgi ClaudeManager Notes'ta.
- Turkiye icin DST riski yazma; Turkiye UTC+3 sabittir.
- Gateway health REST endpoint'i yok.
- Yeni salon modulu eklenince startup seed, register akisi ve `SalonRolePermissions` birlikte guncellenmeli.

## Test Yaklasimi

- Dar backend degisikliklerinde `dotnet build` ve ilgili `dotnet test` calistir.
- Salon/Management UI degisikliklerinde MVC view + ilgili JS + proxy endpoint birlikte kontrol edilmeli.
- Tarayici testi gerekiyorsa lokal uygulamalari Visual Studio ile calistirma prensibine uy; `dotnet run/watch` kullanma.
- Test calistirilamadiysa final raporda acikca soyle.

## CLAUDE.md ile Senkron

`CLAUDE.md` ve `AGENTS.md` ayni ana kurallari tasimali. Birine proje haritasi, deploy kuralı veya kritik hata eklendiginde digerini de guncelle.

## ClaudeManager v2.0 Kullanım Kılavuzu

**Base URL:** `http://127.0.0.1:41847` | **project_id:** `15` | **Version:** `2.0.0`

### Okuma
| Ne | Endpoint |
|----|----------|
| Kurallar/hatalar/tercihler | `GET /api/projects/15/patterns` |
| Yol haritasi | `GET /api/projects/15/roadmap` |
| Yol haritasi ozet | `GET /api/projects/15/roadmap/summary` |
| Yol haritasi istatistik | `GET /api/projects/15/roadmap/stats` |
| Notlar | `GET /api/projects/15/notes` |
| Gunluk | `GET /api/projects/15/journal` |
| Session'lar | `GET /api/projects/15/sessions?page=1&limit=20` |
| Prompt gecmisi | `GET /api/projects/15/prompts?page=1&limit=10` |
| Tool kullanimlari | `GET /api/projects/15/tool-uses?page=1&limit=20` |
| Arama | `GET /api/search?q=TERIM&project=15` |
| Analitik | `GET /api/projects/15/analytics?days=30` |
| Saglik kontrolu | `GET /health` |
| Proje disa aktar | `GET /api/projects/15/export` |

### Yazma
| Ne | Endpoint | Tipler |
|----|----------|--------|
| Pattern | `POST /api/patterns` | rule, mistake, preference |
| Pattern guncelle/sil | `PUT/DELETE /api/patterns/ID` | |
| Not | `POST /api/projects/15/notes` | category: teknik, genel, karar, todo |
| Not guncelle/sil | `PUT/DELETE /api/notes/ID` | |
| Not sabitle | `PUT /api/notes/ID` | `{"is_pinned": 1}` |
| Gunluk | `POST /api/projects/15/journal` | category: genel, teknik, karar, arastirma |
| Gunluk guncelle/sil | `PUT/DELETE /api/journal/ID` | |
| Faz ekle | `POST /api/projects/15/phases` | |
| Faz guncelle/sil | `PUT/DELETE /api/phases/FAZ_ID` | |
| Gorev ekle | `POST /api/phases/FAZ_ID/tasks` | |
| Gorev guncelle/sil | `PUT/DELETE /api/tasks/GOREV_ID` | |
| Roadmap XML import | `POST /api/projects/15/roadmap/import` | XML body |

### Dogru Kullanim Ornekleri

Yeni kural:
```
curl -X POST http://127.0.0.1:41847/api/patterns -H "Content-Type: application/json" \
  -d '{"project_id":15,"type":"rule","title":"BASLIK","description":"ACIKLAMA"}'
```

Yeni hata/ders:
```
curl -X POST http://127.0.0.1:41847/api/patterns -H "Content-Type: application/json" \
  -d '{"project_id":15,"type":"mistake","title":"BASLIK","description":"ACIKLAMA"}'
```

Not:
```
curl -X POST http://127.0.0.1:41847/api/projects/15/notes -H "Content-Type: application/json" \
  -d '{"title":"BASLIK","content":"ICERIK","category":"teknik"}'
```

Gunluk:
```
curl -X POST http://127.0.0.1:41847/api/projects/15/journal -H "Content-Type: application/json" \
  -d '{"title":"BASLIK","content":"ICERIK","category":"teknik"}'
```

Gorev risk raporu:
```
curl -X PUT http://127.0.0.1:41847/api/tasks/GOREV_ID -H "Content-Type: application/json" \
  -d '{"status":"completed","risks":"OLASI RISKLER VE EKSIKLER"}'
```

# Salon UX QA Protokolu

Tarih: 2026-05-25

Bu protokol `SALONUXPLAN-20260525` isleri icin sabah yapilacak runtime smoke ve kabul turunu tarif eder. Kod tarafinda her adim ayri commitlendi; uygulama Visual Studio ile yeniden baslatildiktan sonra asagidaki sirayla gidilmeli.

## Commit Sirasi

| Commit | Kapsam | Kontrol |
|---|---|---|
| `7e2071b` | Marketing konsolidasyonu, public profil selectedPlanId bug fix, default paket dokumanlari | Marketing ana sayfa ve public profil KO hatasi |
| `d2fae66` | Dashboard aksiyon merkezi | `/` salon dashboard |
| `0da97ad` | Randevu bos durum + waitlist koprusu | `/Appointments` |
| `e40d056` | Müşteri cebi/wallet ozeti | `/Clients/Detail/{id}` |
| `46ce9e0` | Public booking funnel + local draft recovery | `/salon/{slug}/book` |
| `123fb66` | Public profil/booking guven sinyalleri | `/salon/{slug}` ve `/book` |
| `afefe8e` | Reports aksiyon onerileri | `/Reports` |
| `262413b` | Services/Marketing yogunluk azaltma | `/Services`, `/Marketing` |
| `7a39ef7` | Rakip esinlenme backlog dokumani | `docs/salon-rakip-esinlenme-backlog.md` |

## Statik Kontroller

Bu kontroller gecti:

```powershell
node --check src\CallCenter.Salon\wwwroot\js\SlnDashboard.js
node --check src\CallCenter.Salon\wwwroot\js\Appointments.js
node --check src\CallCenter.Salon\wwwroot\js\ClientDetail.js
node --check src\CallCenter.Salon\wwwroot\js\PublicBook.js
node --check src\CallCenter.Salon\wwwroot\js\PublicProfile.js
node --check src\CallCenter.Salon\wwwroot\js\Reports.js
node --check src\CallCenter.Salon\wwwroot\js\Services.js
node --check src\CallCenter.Salon\wwwroot\js\Campaigns.js
```

Salon build farkli adimlarda temiz gecti:

```powershell
dotnet build src\CallCenter.Salon\CallCenter.Salon.csproj --no-restore --nologo -v:minimal /m:1 -p:UseAppHost=false
```

Not: runtime browser smoke bilerek sabaha birakildi; uygulama bu gece yeniden baslatilmadi.

## Sabah Smoke Sirasi

1. Visual Studio ile API ve Salon uygulamasini yeniden baslat.
2. Salon URL: `http://localhost:5239/salon/ux-kadikoy-0506013753`
3. Public profil:
   - Sayfa KO hatasi vermeden aciliyor mu?
   - Guven sinyali bandi profil verisine gore gorunuyor mu?
   - Uyelik planlari varsa `selectedPlanId` hatasi yok mu?
   - Online randevu ve waitlist butonlari calisiyor mu?
4. Public booking:
   - Login gerekiyorsa returnUrl korunuyor mu?
   - Hizmet, personel, tarih/saat, bilgi, onay adimlari ilerliyor mu?
   - Guven kartlari policy durumuna gore dogru metin veriyor mu?
   - Yarim bir randevu secimi yapip sayfayi yenileyince "devam et" geri getiriyor mu?
   - Waitlist fallback talep birakabiliyor mu?
5. Dashboard:
   - "Bugun neye bakmaliyim?" aksiyonlari geliyor mu?
   - Aksiyon linkleri ilgili ekrana gidiyor mu?
6. Appointments:
   - Bos aralikta yeni bos durum gorunuyor mu?
   - Aktif waitlist sayisi varsa CTA gorunuyor mu?
   - "Bugunu Goster" takvimi bugune cekiyor mu?
7. Client detail:
   - Müşteri Cebi bandi patlamadan yukleniyor mu?
   - Sadakat, uyelik, seans hakki ve hediye karti bilgisi moduller kapali olsa bile sayfayi bozmuyor mu?
8. Reports:
   - KPI geldikten sonra aksiyon onerileri uretiliyor mu?
   - Stok/Finans/Müşteri tablari acildikca oneriler guncelleniyor mu?
   - Oneri butonlari Products, Marketing, Cash, Expenses gibi hedeflere gidiyor mu?
9. Services:
   - Ustteki araclar dropdown calisiyor mu?
   - Hizmet istatistikleri yukleniyor mu?
   - Yeni hizmet modalinda temel alanlar onde, "Randevu ve kaynak ayarlari" collapse icinde mi?
   - Collapse acilinca kaynak/buffer/on kosul alanlari kayit payloadini bozmuyor mu?
10. Marketing:
    - Niyet kartlari gorunuyor mu?
    - Kart butonlari ilgili pill tabina geciyor mu?
    - Kampanya modalinda hazir segmentler onde, detayli filtreler collapse icinde mi?
    - Yeni kampanya, otomatik hatirlatma, e-posta kampanyasi, uyelik ve hediye kart save akislari regresyon vermiyor mu?

## Kabul Kriterleri

- Konsolda Knockout binding hatasi yok.
- 401 varsa login/returnUrl beklenen sekilde calisiyor.
- 403 varsa modul kilidi ya da yetki mesaji anlasilir.
- Yeni eklenen collapse/dropdown kontroller mobil genislikte metin tasirmiyor.
- Her aksiyon karti bos vaat degil, calisir bir salon ekranina gidiyor.
- Runtime smoke sonunda sorun varsa yeni roadmap task'i acilir; mevcut commitler geri alinmaz, hedefli fix commit atilir.

## Bilinen Kalan Isler

- Public booking funnel su an browser-local MVP; server-side terk edilen booking raporu sonraki is.
- Waitlist auto-fill, queue/self check-in ve utilisation campaign draft `docs/salon-rakip-esinlenme-backlog.md` icinde backlog adaylari olarak kayitli.
- Browser smoke yapilmadan production deploy karari verilmemeli.

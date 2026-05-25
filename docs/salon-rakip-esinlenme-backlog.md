# Salon Rakip Esinlenme Backlogu

Tarih: 2026-05-25

Bu not Square Appointments, SalonIQ ve Zenoti resmi kaynaklarindan urun fikri cikarmak icin hazirlandi. Amac kopyalamak degil; bizde zaten var olan parcalari daha is yapan akislara baglamak.

## Kaynak Ozeti

- Square Appointments: 24/7 online booking, booking site/widget/QR, otomatik SMS/e-posta hatirlatma, no-show policy/prepayment, waitlist ile iptal bosluklarini doldurma ve booking akisi icinde card-on-file/prepayment vurgusu yapiyor.
  Kaynaklar: https://squareup.com/us/en/appointments ve https://squareup.com/us/en/appointments/pricing
- SalonIQ: booking portal/app, automated marketing, rebooking mesajlari, utilisation dashboard ve self check-in/queue kullanimini one cikariyor.
  Kaynaklar: https://www.saloniq.com/features/ ve https://faq.saloniq.com/knowledge/self-check-in-online
- Zenoti: online bookingi web/app/Google/Instagram kanallarina tasiyor; waitlist/turnaway, terk edilen booking geri kazanimi, staff/service bazli kompleks booking ve pazarlama otomasyonu vurguluyor.
  Kaynaklar: https://www.zenoti.com/platform-overview, https://www.zenoti.com/salon/beauty-salon-software ve https://grow.zenoti.com/salon/salon-booking-software

## Bizdeki Durum

| Fikir | Bizde durum | Not |
|---|---|---|
| Public online booking | Var | Public profil ve booking akisi var; guven sinyali ve draft recovery 123fb66/46ce9e0 ile guclendi. |
| Waitlist | Var/kismi | Manuel talep var; otomatik bosluk eslestirme ve musteriye slot onerme yok. |
| No-show/depozito | Var/kismi | Policy ve iyzico depozito var; servis/personel riskine gore dinamik policy yok. |
| Booking funnel recovery | Kismi | Browser-local recovery var; server-side terk edilen booking raporu ve tekrar davet yok. |
| Utilisation driven marketing | Kismi | Reports aksiyon onerileri afefe8e ile basladi; otomatik kampanya taslagi yok. |
| Self check-in/queue | Yok/kismi | Waitlist var, walk-in queue ve musteri self check-in deneyimi yok. |
| Booking channel attribution | Kismi | Public link/QR var; kaynak bazli funnel raporu yok. |
| Multi-service/multi-staff booking UX | Kismi | Combo ve staff akisi var; Zenoti benzeri karmasik servis sure/pricing UX'i daha sade anlatilmali. |

## Backlog Adaylari

| Oncelik | Baslik | Neden | Ilk teslim |
|---|---|---|---|
| P1 | Waitlist auto-fill ve slot onerisi | Square/Zenoti waitlist'i sadece liste degil, bosluk doldurma motoru olarak konumluyor. Bizde waitlist var ama aksiyona donusmesi manuel. | Bos slot olusunca uygun waitlist kayitlarini Appointments ekraninda oner; tek tikla randevuya cevir. |
| P1 | Server-side booking recovery | Zenoti terk edilen booking'i geri kazanma mesajiyla bagliyor. Bizde local draft var, salon sahibi raporlayamiyor. | PublicBook funnel eventlerini API'ye yaz; Marketing icinde "yarim kalan randevular" segmenti ac. |
| P1 | Utilisation campaign draft | SalonIQ utilisation dashboard'u bos gun/personel icin pazarlamaya bagliyor. Bizde Reports aksiyon onerisi var. | Reports aksiyonundan Marketing kampanya modalini hazir segment ve mesaj taslagiyla ac. |
| P1 | Queue/self check-in mode | SalonIQ self check-in walk-in salonlar icin kuyruk akisi sunuyor. Turkiye'de berber/nail bar icin is gorur. | Public profil uzerinden "siraya gir" modu; salonda Waitlist/Queue ekraninda canli sira. |
| P2 | No-show policy risk seviyesi | Square no-show/prepayment'i booking kararina bagliyor. Bizde policy genel. | Hizmet bazli depozito/yuzde/flat fee secimi ve riskli musteri icin daha guclu uyari. |
| P2 | Booking kaynak/kanal raporu | Square/Zenoti booking'i site, QR, Google, Instagram gibi kanallara yayiyor. Bizde QR/link var ama kaynak takibi yok. | Public linklere `source` parametresi; Reports'ta booking kaynak dagilimi. |
| P2 | Pre-book form ve dosya toplama | Square booking sirasinda form/contract toplama mesajini one cikariyor. Bizde KVKK/onam altyapisi var. | Hizmete bagli public on-form; musteri kartina otomatik eklenen yanitlar. |
| P2 | Multi-service booking polish | Zenoti karmasik servis zamanlari ve staff/pricing varyantlarini iyi pazarlıyor. Bizde combo var ama anlatimi guclendirilmeli. | Booking'te combo kartlarini "set/paket" gibi goster; toplam sure/fiyat/personel ihtiyacini daha acik yaz. |

## TR/Pazar Uygunlugu

- SMS ve WhatsApp benzeri kanallar Turkiye'de daha kritik; e-posta ikinci planda kalabilir.
- Depozito/no-show akisi KVKK, mesafeli satis ve iyzico deneyimiyle net anlatilmali; gizli card-on-file iddiasi kurulmamalı.
- Queue/self check-in ozellikle randevusuz calisan berber, tirnak ve hizli bakim isleri icin degerli.
- Branded mobile app fikri simdilik P3; public PWA/QR/link deneyimini guclendirmek daha az maliyetli.

## Roadmap Baglantisi

- `SALONUXGAP.6` waitlist akillandirma icin ana aday: waitlist auto-fill + queue/self check-in.
- `SALONUXPLAN.8` Reports aksiyon onerileri icin sonraki aday: utilisation campaign draft.
- `SALONUXPLAN.6` public booking recovery icin sonraki aday: server-side funnel + Marketing segmenti.

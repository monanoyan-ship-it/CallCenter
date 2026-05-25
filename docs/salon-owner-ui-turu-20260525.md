# Salon Owner UI Turu

Tarih: 2026-05-25

Bu not `SALONUXGAP.2` icin hazirlandi. Amac owner yetkili salon kullanicisinin ana ekranlarda ne gordugunu ve son UX commitlerinden sonra hangi kabul noktalarina bakilacagini tek yerde toplamak. Canli screenshot referansi `docs/salon-live-screenshot-index.md`; yeni runtime smoke ise sabah `docs/salon-ux-qa-protokolu-20260525.md` sirasi ile yapilacak.

## Tur Ozeti

| Ekran | Route | Mevcut is | Son dokunus | Sabah bakilacak nokta |
|---|---|---|---|---|
| Public profil | `/salon/{slug}` | Salon vitrin, hizmetler, uyelikler, yorumlar, waitlist | `123fb66` | Guven sinyalleri gercek policy/yorum/iletisim verisine gore geliyor mu? |
| Public booking | `/salon/{slug}/book` | Online randevu, hizmet/personel/tarih/slot, musteri bilgisi, waitlist | `46ce9e0`, `123fb66` | Draft recovery bandi, waitlist fallback ve login returnUrl bozulmuyor mu? |
| Dashboard | `/` | KPI, modul/abonelik, gunluk ozet | `d2fae66` | "Bugun neye bakmaliyim?" kartlari gercek veriye gore aksiyon uretiyor mu? |
| Randevular | `/Appointments` | Takvim, filtre, randevu CRUD | `0da97ad` | Bos aralikta yeni randevu/waitlist/bugun CTA'lari gorunuyor mu? |
| Musteriler | `/Clients` ve detay | Musteri listesi, notlar, randevu/satis gecmisi | `e40d056` | Musteri Cebi sadakat/uyelik/paket/hediye kartini bozmadan topluyor mu? |
| Hizmetler | `/Services` | Hizmet, kategori, combo, kaynak, seans paket baglantisi | `262413b` | Araclar dropdown ve ileri ayarlar collapse mobilde tasma yapmiyor mu? |
| Hizli satis | `/Sales` | Sepet, hizmet/urun satisi, personel, odeme | Bu sprintte degismedi | Randevu/adisyon/stok/seans baglantilari regresyon vermiyor mu? |
| Adisyonlar | `/Invoices` | Adisyon, odeme, iade, cari baglanti | Bu sprintte degismedi | Yeni wallet/reports aksiyonlari mevcut adisyon akisini bozmuyor mu? |
| Kasa | `/Cash` | Kasa, acilis/kapanis, hareketler | Reports aksiyon hedefi oldu | Dashboard/Reports kartindan gelince hedef ekran anlasilir mi? |
| Urunler | `/Products` | Urun, marka, stok, hareket, sube bakiyesi | Reports aksiyon hedefi oldu | Dusuk stok onerisi dogru ekrana gidiyor mu? |
| Tedarikciler | `/Suppliers` | Tedarikci, siparis, borc/alacak | Reports aksiyon hedefi oldu | Tedarikci borcu onerisi owner icin anlamli mi? |
| Marketing | `/Marketing` | Kampanya, otomasyon, e-posta, winback, sadakat, uyelik, hediye kart, yorum | `7e2071b`, `262413b` | Niyet kartlari dogru tablari aciyor; save akislari kapanip listeyi yeniliyor mu? |
| Reports | `/Reports` | Satis, doluluk, stok, finans, musteri metrikleri | `afefe8e` | Metriklerden cikan aksiyonlar bos vaat degil mi? |
| Bekleme listesi | `/Waitlist` | Aktif talepler, bilgilendir, randevuya cevir | `0da97ad` Appointments koprusu | Manuel randevuya cevirme var; auto-fill henuz backlog. |
| Subeler | `/Branches` | Cok sube yonetimi | Bu sprintte degismedi | Owner global/sube scoped davranis korunuyor mu? |
| Sayfa ayarlari/profil | `/PageSettings`, `/Profile` | Public link, salon bilgileri, calisma saatleri | Public guven kartlarini besliyor | Profil verisi eksikse public tarafta yanlis iddia cikmiyor mu? |

## Owner Hikayesi

1. Owner public profilden baslar: salonun guven veren vitrin, hizmet, yorum ve online randevu sinyalleri gorunmeli.
2. Dashboard'a gecer: tek tek rapor aramak yerine bugun takip edecegi isler listelenmeli.
3. Randevu ekraninda bosluk varsa yeni randevu veya waitlistten doldurma koprusu gorunmeli.
4. Musteri detayinda musteriye ait haklar, sadakat, paket ve hediye karti tek ozetle okunmali.
5. Hizmetler ekraninda temel isler onde, ileri kaynak/seans ayarlari ihtiyac halinde acilmali.
6. Marketing ekraninda teknik modul adlari yerine "musteriye ulas", "sadakat ve gelir", "itibar" gibi is niyetleri gorunmeli.
7. Reports ekraninda metrikler sadece sayi olarak kalmamali; Products, Marketing, Cash veya Expenses gibi hedef ekrana goturen aksiyon uretmeli.

## Gorsel Esinlenme Notu

- Kart sayisi artirmak yerine aksiyonun ne oldugunu netlestirmek daha degerli.
- Public tarafta guven sinyali salon verisinden gelmeli; bos/yanlis veriyle iddia uretmemeli.
- Owner tarafinda yogun ekranlar kalabilir ama ilk bakista "hangi is yapiliyor" belli olmali.
- Hizmet ve marketing gibi kalabalik formlarda ileri alanlar collapse/dropdown altinda tutulmali.

## Kalan Risk

Bu tur dokuman ve kod yuzeyi ile tamamlandi; calisan local Salon process bu gece restart edilmedi. Sabah runtime smoke olmadan production deploy karari verilmemeli. Smoke sirasinda yeni hata gorulurse yeni roadmap task'i acilip hedefli fix commit atilmali.

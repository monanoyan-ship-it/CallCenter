# CorpLynk Salon Demo Veri ve Çekim Runbook'u

Sürüm: Taslak v0.1  
Amaç: Tanıtım videosu için temiz demo veri hazırlamak ve ekran kayıtlarını aynı sırayla almak  
Kapsam: Salon web uygulaması, public salon sayfası ve müşteri paneli

## 1. Çekim Prensipleri

- Gerçek müşteri, telefon, e-posta, ödeme veya banka bilgisi kullanılmaz.
- Tüm demo kayıtları sahte ad, `example.com` e-posta ve demo telefonlarla oluşturulur.
- Tarayıcı zoom oranı çekim boyunca değişmez.
- Ekran kayıtları kısa klipler halinde alınır; final kurgu bu kliplerden yapılır.
- Bir ekranda hata, yüklenemeyen alan, gerçek veri veya gereksiz bildirim görünürse klip tekrar alınır.

## 2. Önerilen Demo Veri Seti

### 2.1 Salon ve Şubeler

Salon adı:

- CorpLynk Demo Güzellik

Şubeler:

| Şube | Slug önerisi | Adres kısa metni |
| --- | --- | --- |
| Merkez Şube | corplynk-demo-merkez | Çekmeköy, İstanbul |
| Kadıköy Şube | corplynk-demo-kadikoy | Kadıköy, İstanbul |

Çalışma saatleri:

- Pazartesi-Cumartesi: 09:00-19:00
- Pazar: Kapalı

### 2.2 Personel

| Ad Soyad | Ünvan | Şube | Hizmetler |
| --- | --- | --- | --- |
| Ayşe Uzman | Cilt Bakım Uzmanı | Merkez Şube | Cilt Bakımı, Lazer Epilasyon |
| Mert Kuaför | Saç Tasarım Uzmanı | Merkez Şube | Saç Kesim, Fön, Saç Boyama |
| Elif Nail Artist | Nail Artist | Kadıköy Şube | Manikür, Kalıcı Oje |

Personel public görünürlükleri açık olmalı. Video için en az iki personele fotoğraf eklenmesi iyi görünür.

### 2.3 Hizmetler

| Kategori | Hizmet | Süre | Fiyat |
| --- | --- | --- | --- |
| Saç | Saç Kesim | 45 dk | 650 TL |
| Saç | Fön | 30 dk | 350 TL |
| Saç | Saç Boyama | 120 dk | 2.200 TL |
| Cilt Bakımı | Medikal Cilt Bakımı | 60 dk | 1.400 TL |
| Epilasyon | Lazer Epilasyon | 45 dk | 1.800 TL |
| Tırnak | Kalıcı Oje | 60 dk | 900 TL |

Combo örneği:

- Bakım Günü Paketi: Medikal Cilt Bakımı + Fön

Seans tanımı:

- Lazer Epilasyon 6 Seans
- Toplam seans: 6
- Fiyat: 8.500 TL
- Geçerlilik: 365 gün

### 2.4 Ürünler ve Stok

| Ürün | Kategori | Marka | Satış fiyatı | Stok |
| --- | --- | --- | --- | --- |
| Keratin Bakım Serumu | Bakım | DemoCare | 780 TL | 12 |
| Renk Koruyucu Şampuan | Saç | DemoCare | 420 TL | 8 |
| Cilt Temizleme Köpüğü | Cilt | PureDemo | 540 TL | 4 |

Kritik stok için bir üründe stok düşük bırakılabilir. Dashboard ve stok ekranında daha anlamlı görünür.

### 2.5 Müşteriler

| Ad Soyad | Telefon | E-posta | Not |
| --- | --- | --- | --- |
| Deniz Yılmaz | 0500 100 10 01 | deniz.yilmaz@example.com | Cilt bakım müşterisi |
| Elif Kaya | 0500 100 10 02 | elif.kaya@example.com | Lazer epilasyon paketi |
| Burcu Demir | 0500 100 10 03 | burcu.demir@example.com | Sadakat kampanyası hedefi |

Müşteri detayında gösterilecek örnek notlar:

- Cilt tipi: Karma
- Alerji: Bilinen yok
- Tercih: Akşam saatleri
- Tedavi notu: İlk seansta hassasiyet gözlemlenmedi

### 2.6 Finans ve Pazarlama Verileri

Hazırlanacak örnekler:

- Hızlı satış: Deniz Yılmaz için Medikal Cilt Bakımı + Keratin Bakım Serumu
- Adisyon: 2.180 TL
- Masraf: Temizlik gideri, 950 TL
- Hediye kartı: 2.000 TL, alıcı Burcu Demir
- Üyelik planı: VIP Bakım Üyeliği, aylık 2.500 TL
- Sadakat ayarı: Her 100 TL harcamaya 5 puan
- Kampanya: Mayıs Bakım Kampanyası
- E-posta kampanyası: Yaz Bakım Randevuları Başladı
- Geri kazanım kuralı: 45 gündür gelmeyen müşteriler

## 3. Demo Veri Hazırlama Sırası

1. Salon profili ve sayfa ayarlarını doldur.
2. Merkez ve Kadıköy şubelerini oluştur.
3. Hizmet kategorileri ve hizmetleri oluştur.
4. Personelleri oluştur, şube ve hizmet yetkinliklerini bağla.
5. Ürün, marka, kategori ve stok verilerini hazırla.
6. Tedarikçi ekle.
7. Demo müşterileri oluştur.
8. Lazer Epilasyon 6 Seans tanımını oluştur.
9. Deniz Yılmaz için randevu oluştur.
10. Elif Kaya için seans paketi satışı oluştur.
11. Hızlı satıştan ürün + hizmet satışı yap.
12. Kasa, adisyon ve dashboard ekranlarını kontrol et.
13. Public salon sayfasında hizmet/personel/randevu akışını test et.
14. Raporlar ekranında tarih filtresiyle dolu veri göründüğünü kontrol et.

## 4. Ekran Kayıt Sırası

Her klip ayrı kaydedilsin. Dosya adları bu sırayı korusun.

| Klip | Dosya adı | Ekran | Süre | Gösterilecek aksiyon |
| --- | --- | --- | --- | --- |
| 01 | `01-dashboard.mp4` | Dashboard | 6 sn | Günlük özet, ciro, randevu, kritik stok |
| 02 | `02-randevu.mp4` | Randevular | 8 sn | Yeni randevu oluşturma ve durum güncelleme |
| 03 | `03-hizli-satis.mp4` | Hızlı Satış | 9 sn | Hizmet/ürün sepete ekleme, ödeme seçme |
| 04 | `04-musteri-karti.mp4` | Müşteriler | 8 sn | Müşteri detay, geçmiş, not, seans bilgisi |
| 05 | `05-hizmetler.mp4` | Hizmetler | 6 sn | Hizmet, seans tanımı ve combo görünümü |
| 06 | `06-personel.mp4` | Personel | 5 sn | Personel kartı, hizmet yetkinliği |
| 07 | `07-stok.mp4` | Ürünler | 5 sn | Şube bazlı stok ve kritik stok |
| 08 | `08-kasa-adisyon.mp4` | Kasa/Adisyon | 6 sn | Ödeme, adisyon, kasa hareketi |
| 09 | `09-public-booking.mp4` | Public sayfa | 8 sn | Hizmet/personel/saat seçimi |
| 10 | `10-pazarlama.mp4` | Kampanya/Sadakat | 6 sn | Kampanya, üyelik, sadakat kartları |
| 11 | `11-raporlar.mp4` | Raporlar | 5 sn | Satış/personel/stok raporu |
| 12 | `12-cta.mp4` | Landing/logo | 4 sn | Logo, demo CTA |

## 5. Klip Bazlı Çekim Notları

Canlı screenshot referansı:

- Screenshot index: `docs/salon-live-screenshot-index.md`
- Canlı görseller: `docs/assets/salon-live-screenshots/`
- 2026-05-21 seti canlı Salon demo hesabından alınmıştır.
- Bu set video kurgusu için ekran sırasını doğrular; final müşteri-facing kayıt öncesi demo veri isimleri temizlenmelidir.

### 5.1 Dashboard

Hedef:

- İlk bakışta uygulamanın dolu ve operasyon odaklı göründüğünü göstermek.

Kadraj:

- Sol menü görünür kalsın.
- KPI kartları ve bugünün randevuları aynı anda görünsün.

Tekrar çekim sebebi:

- Boş dashboard
- Kritik stok veya randevu alanının yüklenememesi
- Gerçek kullanıcı adı görünmesi

### 5.2 Randevular

Hedef:

- Randevunun müşteri, hizmet, personel ve saat ilişkisiyle yönetildiğini göstermek.

Aksiyon:

1. Yeni randevu modalını aç.
2. Deniz Yılmaz müşterisini seç.
3. Medikal Cilt Bakımı hizmetini seç.
4. Ayşe Uzman personelini seç.
5. Saat seçimini göster.
6. Kaydet veya kaydedilmiş randevunun durumunu değiştir.

### 5.3 Hızlı Satış

Hedef:

- POS hızını göstermek.

Aksiyon:

1. Müşteri seç.
2. Hizmet ekle.
3. Ürün ekle.
4. Toplam tutarı göster.
5. Ödeme yöntemini göster.

Ödeme ekranında gerçek kart bilgisi gösterilmez.

### 5.4 Müşteri Kartı

Hedef:

- Salonun müşteri hafızası olduğunu göstermek.

Gösterilecek alanlar:

- İletişim
- Randevu geçmişi
- Tedavi notu
- Seans/paket bilgisi
- Önce/sonra veya onay formu alanı

### 5.5 Public Booking

Hedef:

- Müşterinin kendi başına randevu alabildiğini göstermek.

Aksiyon:

1. Public salon sayfasını aç.
2. Hizmet seç.
3. Personel seç.
4. Tarih/saat seç.
5. Müşteri giriş/kayıt ekranına kadar ilerle.

Kayıt sırasında gerçek telefon/e-posta kullanılmaz.

## 6. Kurgu Sırası

Önerilen final sırası:

1. Problem görüntüsü
2. Dashboard
3. Randevu
4. Hızlı satış
5. Müşteri kartı
6. Public booking
7. Pazarlama/sadakat
8. Raporlar
9. Logo ve CTA

Müzik girişte hafif başlamalı, dashboard ile ritim yükselmeli, CTA bölümünde sakin kapanmalıdır.

## 7. Ekran Üstü Yazı Listesi

Kullanılacak kısa metinler:

- Tüm operasyon tek panelde
- Randevu planla, takip et, tamamla
- Satıştan kasaya kesintisiz akış
- Müşteri geçmişi her zaman elinizin altında
- Online randevu sayfanız hazır
- Tekrar satış için sadakat ve kampanya araçları
- Şube, personel, stok ve finans raporları
- CorpLynk Salon ile salonunuzu net yönetin

## 8. Teknik Kontrol

Kayıt öncesi:

- Tarayıcı zoom: 90% veya 100%
- Çözünürlük: 1920x1080
- Bookmark bar kapalı
- Bildirimler kapalı
- Demo kullanıcı adı gerçek kişiyi çağrıştırmıyor
- Public URL ve e-posta alanlarında gerçek veri yok

Kayıt sonrası:

- Klipler ses içermiyorsa sorun değil; voice-over sonradan eklenecek.
- Her klibin ilk ve son 1 saniyesinde kesmeye uygun boşluk olmalı.
- Fare imleci önemli butonlarda 0.5-1 sn beklemeli.
- Modal kapanışları hızlı kesilmemeli.

## 9. Teslim Dosyaları

Önerilen klasör:

`media/salon-tanitim/`

Önerilen dosyalar:

- `raw/01-dashboard.mp4`
- `raw/02-randevu.mp4`
- `raw/03-hizli-satis.mp4`
- `raw/04-musteri-karti.mp4`
- `raw/05-hizmetler.mp4`
- `raw/06-personel.mp4`
- `raw/07-stok.mp4`
- `raw/08-kasa-adisyon.mp4`
- `raw/09-public-booking.mp4`
- `raw/10-pazarlama.mp4`
- `raw/11-raporlar.mp4`
- `raw/12-cta.mp4`
- `exports/corplynk-salon-tanitim-1080p.mp4`
- `exports/corplynk-salon-tanitim-15sn.mp4`
- `exports/corplynk-salon-tanitim-tr.srt`

## 10. Final Kabul Kriteri

- Video 60-75 saniye aralığında.
- Ürün adı ilk 10 saniyede görünüyor.
- En az 6 gerçek uygulama ekranı var.
- Public müşteri akışı görünüyor.
- Hızlı satış ve randevu mutlaka var.
- Gerçek müşteri/veri yok.
- Altyazı okunabilir.
- Mobil kırpımda CTA kesilmiyor.
- Video sonunda `corplynk.com` görünüyor.

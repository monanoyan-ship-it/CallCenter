# CorpLynk Salon Default Paket Kullanım Akışı

Sürüm: Taslak v0.1  
Amaç: Default paketle giriş yapan salon sahibine, içeride hangi ekranda ne yapacağını göstermek  
Kapsam: `https://sln.corplynk.com` login sonrası salon paneli  
Kapsam dışı: public/discover ekranları, müşteri mobil/panel akışı, ücretli büyüme modülleri

## 1. Net Çerçeve

Bu doküman bir pazarlama sayfası değil; yeni salon kullanıcısının ilk gün ürüne girince hangi sırayla ilerleyeceğini anlatan kullanım kılavuzudur.

Default pakette hedeflenen ilk deneyim:

1. Eski müşteri ve operasyon verisini içeri al.
2. Şube, hizmet, personel ve ürün temelini kontrol et.
3. Randevu, müşteri, satış, adisyon ve kasa akışını günlük kullanıma al.
4. Modüller ekranından abonelik/paket durumunu takip et.

Not: Gösterge Paneli artık fiyat/abonelik anlatımının yeri değildir. Abonelik ve fiyat bilgisi Modüller ekranında anlatılır.

## 2. Eski Veri Aktarımı

Salon tarafında eski dataları toplu içeri almak için `Yönetim > Veri Aktarımı` ekranı bulunur. Bu ekran Excel dosyasını önce satır satır önizler; hatalı, mükerrer ve aktarılabilir satırları kullanıcıya göstermeden kayıt oluşturmaz.

### Veri Aktarımı

Amaç:

- Excel ile eski müşteri listesini almak.
- Hizmetleri, fiyatları ve süreleri almak.
- Personel listesini ve hizmet yetkinliklerini almak.
- Ürün/stok açılış bakiyelerini almak.
- Çok şubeli yapıda kayıtları doğru şubeyle eşleştirmek.

MVP akışı:

1. Şablon indir: Müşteriler, Hizmetler, Personel, Ürünler/Stok.
2. Dosya yükle: XLSX.
3. Otomatik kolon okuma: Telefon, ad soyad, hizmet adı, fiyat, süre, şube gibi alanlar Türkçe/İngilizce başlıklardan tanınır.
4. Ön izleme yap: Hatalı satır, eksik zorunlu alan, tekrar kayıt ve şube eşleşmesi gösterilir.
5. İçeri aktar: Sadece doğrulanan satırlar kaydedilir.
6. Sonuç raporu göster: Eklenen, atlanan, mükerrer ve hatalı satırlar.

Duplicate kuralları:

- Müşteri: telefon birincil, e-posta ikincil eşleştirme.
- Hizmet: aynı kategori/hizmet adı çakışması uyarı verir.
- Personel: kullanıcı adı/e-posta çakışması uyarı verir.
- Ürün: barkod varsa barkod, yoksa ürün adı + şube eşleşmesi.

Not: Bu ilk sürüm manuel kolon eşleştirme ve geçmiş randevu/satış importu yapmaz. Eski sistemden gelen çekirdek listeleri içeri almak içindir.

## 3. İlk Kurulum Sırası

### 3.1 Gösterge Paneli

Ne için kullanılır:

- Bugünün randevuları, müşteri sayısı, ciro, aktif personel ve kritik stok gibi günlük operasyon özetini görmek.
- Güne başlarken hızlı durum kontrolü yapmak.

Ne anlatılmalı:

- Burası fiyat/paket ekranı değil.
- Kullanıcı “bugün ne oluyor?” sorusunun cevabını burada alır.

Görsel: `docs/assets/salon-wide-screenshots/01-dashboard.png`

### 3.2 Şubeler

Ne için kullanılır:

- Salonun şubelerini, adreslerini, çalışma saatlerini ve temel görünürlük ayarlarını yönetmek.
- Çok şubeli kullanımda operasyonun hangi şubeden yürüdüğünü belirlemek.

İlk kurulumda:

- Merkez şube bilgisi kontrol edilir.
- Çalışma saatleri girilir.
- Varsa diğer şubeler eklenir.

Görsel: `docs/assets/salon-wide-screenshots/11-branches.png`

### 3.3 Salon Profili

Ne için kullanılır:

- Salonun genel firma/profil bilgilerini düzenlemek.
- Logo, açıklama, iletişim ve marka bilgilerini toparlamak.

İlk kurulumda:

- Salon adı, açıklama, iletişim ve sosyal medya bilgileri kontrol edilir.
- Public tarafa çıkacak içerik daha sonra ayrıca ele alınır; bu dokümanda ana odak içerideki paneldir.

Görsel: `docs/assets/salon-wide-screenshots/10-profile.png`

### 3.4 Hizmetler

Ne için kullanılır:

- Verilen hizmetleri, fiyatları, süreleri, kategorileri ve seans takibini tanımlamak.
- Randevu ve satış akışının temel listesini oluşturmak.

İlk kurulumda:

- En çok verilen hizmetler girilir.
- Süre ve fiyatlar doğrulanır.
- Seanslı hizmet varsa varsayılan seans sayısı belirlenir.

Günlük kullanımda:

- Fiyat değişikliği, hizmet aktif/pasif durumu ve yeni hizmet ekleme buradan yapılır.

Görsel: mevcut geniş set içinde `05-services.png` var, yeni 1600px set için tekrar alınmalı.

### 3.5 Personel

Ne için kullanılır:

- Personel hesaplarını, rollerini, şubelerini ve verebildikleri hizmetleri yönetmek.
- Randevuda hangi personelin hangi hizmet için seçilebileceğini belirlemek.

İlk kurulumda:

- Salon sahibi dışındaki personel hesapları açılır.
- Her personele doğru şube ve hizmet yetkinliği atanır.
- Public görünürlük ve çalışma saatleri kontrol edilir.

Görsel: geniş screenshot setinde eksik; tekrar alınmalı.

### 3.6 Ürünler ve Stok

Ne için kullanılır:

- Satılan veya hizmette tüketilen ürünleri tanımlamak.
- Şube bazlı stok, barkod, marka, fiyat ve kritik stok takibi yapmak.

İlk kurulumda:

- Ürün kartları oluşturulur.
- Stok açılış bakiyesi şubeye göre girilir.
- Kritik stok seviyeleri belirlenir.

Günlük kullanımda:

- Satış ve reçete tüketimi sonrası stok takibi yapılır.

Görsel: `docs/assets/salon-wide-screenshots/05-products.png`

### 3.7 Randevular

Ne için kullanılır:

- Müşteri, hizmet, personel, şube, tarih ve saat seçerek randevu oluşturmak.
- Randevu durumlarını takip etmek.

Günlük kullanımda:

1. Müşteri seçilir veya hızlı müşteri kaydı açılır.
2. Hizmet/personel/saat seçilir.
3. Randevu oluşturulur.
4. Durum güncellenir: bekliyor, onaylandı, geldi, tamamlandı, iptal, gelmedi.

Görsel: `docs/assets/salon-wide-screenshots/02-appointments.png`

### 3.8 Müşteriler

Ne için kullanılır:

- Müşteri kartı, iletişim bilgileri, notlar, geçmiş randevular ve satış geçmişini yönetmek.
- Müşteri hafızasını tek yerde toplamak.

Günlük kullanımda:

- Yeni müşteri eklenir.
- Telefon/e-posta bilgisi güncellenir.
- Not, sağlık bilgisi, onay formu ve geçmiş kayıtlar kontrol edilir.

Görsel: `docs/assets/salon-wide-screenshots/04-clients.png`

### 3.9 Hızlı Satış

Ne için kullanılır:

- Hizmet ve ürün satışını hızlıca tamamlamak.
- Ödeme yöntemi seçerek adisyon ve kasa hareketi oluşturmak.

Günlük kullanımda:

1. Müşteri seçilir.
2. Hizmet veya ürün sepete eklenir.
3. Personel seçimi gerekiyorsa atanır.
4. Ödeme alınır.
5. Satış tamamlanır.

Görsel: `docs/assets/salon-wide-screenshots/03-sales.png`

### 3.10 Adisyonlar

Ne için kullanılır:

- Satış kayıtlarını, ödeme durumunu, iptal/iade akışlarını ve detay kalemlerini görmek.

Günlük kullanımda:

- Açık adisyonlar kontrol edilir.
- Ödeme durumu izlenir.
- Hatalı satışlarda işlem geçmişine göre iptal/iade yapılır.

Görsel: `docs/assets/salon-wide-screenshots/06-invoices.png`

### 3.11 Kasa

Ne için kullanılır:

- Günlük nakit/kart hareketlerini ve kasa bakiyesini takip etmek.
- Gün sonu kapanışını hazırlamak.

Günlük kullanımda:

- Satışlardan gelen hareketler izlenir.
- Manuel giriş/çıkışlar açıklamayla girilir.
- Gün sonunda kasa kontrol edilir.

Görsel: `docs/assets/salon-wide-screenshots/07-cash.png`

### 3.12 Reçeteler

Ne için kullanılır:

- Bir hizmet sırasında tüketilen ürünleri tanımlamak.
- Hizmet tamamlandığında stok düşümünün doğru ürünlerden yapılmasını sağlamak.

Örnek:

- Saç boyama hizmeti için boya, oksidan ve bakım ürünü miktarları tanımlanır.

Görsel: geniş screenshot setinde eksik; tekrar alınmalı.

### 3.13 Bekleme Listesi

Ne için kullanılır:

- Müşterinin istediği saat doluysa uygun boşluk oluştuğunda geri dönmek.

Günlük kullanımda:

- Müşteri, hizmet, tarih/saat aralığı ve not girilir.
- Boşluk oluştuğunda uygun müşteri aranır veya mesajlanır.

Görsel: geniş screenshot setinde eksik; tekrar alınmalı.

### 3.14 No-Show ve Onay Formları

Ne için kullanılır:

- Gelmeme/iptal kurallarını ve müşteri onay metinlerini yönetmek.
- KVKK, hizmet onayı ve işlem öncesi rıza kayıtlarını düzenlemek.

İlk kurulumda:

- İptal süresi ve gelmeme politikası belirlenir.
- Kullanılacak onay formları hazırlanır.

Görsel: geniş screenshot setinde eksik; tekrar alınmalı.

### 3.15 Personel Fiyatları

Ne için kullanılır:

- Aynı hizmetin farklı personelde farklı fiyatla satılacağı durumları yönetmek.
- Komisyon/hasılat paylaşımı için temel fiyat kuralı oluşturmak.

Görsel: geniş screenshot setinde eksik; tekrar alınmalı.

### 3.16 Modüller

Ne için kullanılır:

- Aktif paketleri, abonelik durumunu, aylık toplamı ve ödeme durumunu görmek.
- Default paket dışındaki modülleri incelemek veya satın almak.

Ne anlatılmalı:

- Fiyat ve abonelik bilgisi burada durur.
- Gösterge Paneli günlük operasyon içindir; Modüller ekranı abonelik/ödeme bilgisidir.

Görsel: `docs/assets/salon-wide-screenshots/08-modules.png`

## 4. Default Paket Dışında Bırakılacaklar

Bu kullanım kılavuzu ve eğitim videosunda şu başlıklar ana akışa konmamalı:

- Public/discover ekranları.
- Müşteri paneli ve müşteri mobil uygulaması.
- Kampanya, üyelik, sadakat, yorum, geri kazanım gibi ücretli büyüme modülleri.
- Raporlar, tedarikçiler, masraflar, hediye kartları ve gelişmiş kurumsal ekranlar.

Bu ekranlar ayrı “büyüme modülleri” veya “ileri kullanım” videosuna taşınmalı.

## 5. Eğitim Videosu İçin Sıra

Önerilen 6-8 dakikalık kullanım videosu:

1. Giriş ve default paket menüsü: 30 sn
2. Eski veri aktarımı ihtiyacı: 45 sn
3. Şube, profil, hizmet ve personel kurulumu: 2 dk
4. Randevu ve müşteri yönetimi: 1.5 dk
5. Hızlı satış, adisyon ve kasa: 1.5 dk
6. Ürün/stok ve reçete mantığı: 1 dk
7. Modüller ekranı: 30 sn

Kısa sosyal/video versiyonu yapılacaksa marketing sloganı yerine ürün içi akış kullanılmalı: “Bugün randevu al, satış yap, kasayı kapat, stokunu gör.”

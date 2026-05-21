# CorpLynk Salon Kullanım Kılavuzu

Sürüm: Taslak v0.1  
Hedef kullanıcı: salon sahibi, şube yöneticisi ve salon personeli  
Uygulama: `https://sln.corplynk.com`

## 1. Genel Bakış

CorpLynk Salon; randevu, müşteri, hızlı satış, kasa, stok, personel, paket/üyelik, pazarlama ve raporlama akışlarını tek panelde toplar.

Temel mantık şudur:

- Salon sahibi tüm şubeleri ve yönetim ekranlarını görür.
- Şube yöneticisi kendi şubesinin operasyonunu yönetir.
- Personel günlük randevu, müşteri ve satış akışlarını kullanır.
- Müşteriler public salon sayfasından randevu alabilir, kullanıcı panelinden randevu ve ödeme bilgilerini takip edebilir.

Sol menüde görünen ekranlar kullanıcının rolüne, aktif modüllere ve şube yetkisine göre değişir.

## 2. İlk Kurulum

Yeni salon hesabı açıldıktan sonra canlı kullanıma geçmeden önce bu sırayla ilerleyin.

### 2.1 Salon Profili

Menü: Yönetim > Salon Profili

Burada salonun genel tanıtım bilgileri düzenlenir.

- Açıklama
- Web sitesi
- Sosyal medya bilgileri
- Yayın durumu
- Faturalama tipi

Profil bilgileri public salon sayfasında ve müşteri tarafındaki görünümde kullanılır.

### 2.2 Şubeler

Menü: Yönetim > Şubeler

Şubeler günlük operasyonun merkezidir. Randevu, kasa, stok, müşteri ve personel kayıtları şube bilgisiyle çalışır.

Her şube için şunları doldurun:

- Şube adı
- Adres ve iletişim bilgileri
- Public görünürlük
- Çalışma saatleri
- Public sayfa/slug bilgisi
- Kapak, logo ve galeri görselleri

Çok şubeli salonlarda salon sahibi tüm şubeleri görebilir. Şube kullanıcısı ise yalnızca kendi şubesinin verisiyle çalışır.

### 2.3 Hizmetler

Menü: Operasyon > Hizmetler

Önce kategori, sonra hizmet oluşturun.

Hizmette önemli alanlar:

- Hizmet adı
- Kategori
- Süre
- Fiyat
- Tampon süre
- İşlem/bekleme süresi
- Ek hizmet veya varyant bilgisi
- Konsültasyon veya patch test gereksinimi
- Kaynak/oda/cihaz gereksinimi
- Seans takip tanımı
- Combo hizmet tanımı

Güzellik merkezi akışında seanslı işler hizmet satışı olarak düşünülür. Örneğin 6 seans epilasyon tek bir hizmet satışı olarak alınır, sonra seanslar randevu/adisyon üzerinden takip edilir.

### 2.4 Personel

Menü: Operasyon > Personel

Personel kartında kullanıcı hesabı, rol, şube, hizmet yetkinliği ve public görünürlük ayarlanır.

Kontrol listesi:

- Ad soyad ve e-posta
- Kullanıcı adı ve şifre
- Rol
- Şube
- Verebildiği hizmetler
- Aktif/pasif durumu
- Public sayfada görünüp görünmeyeceği
- Çalışma saatleri
- Vardiya, izin, timesheet ve bordro kayıtları

Personel public randevu ekranında görünecekse fotoğraf ve uzmanlık bilgisini de doldurun.

### 2.5 Kasa ve Ödeme Bilgileri

Menü: Finans > Kasa  
Menü: Yönetim > Ödeme Bilgileri

Kasa ekranı günlük giriş/çıkışları ve gün sonu kapanışını takip eder. Ödeme bilgileri ekranı ise online ödeme ve pazaryeri/hesap bilgileri için kullanılır.

Canlı satışa geçmeden önce:

- En az bir kasa oluşturun.
- Kasanın doğru şubeye bağlı olduğunu kontrol edin.
- Ödeme yöntemlerini test edin.
- Online ödeme kullanılıyorsa ödeme bilgilerini tamamlayın.

### 2.6 Public Sayfa ve Randevu Kuralları

Menü: Yönetim > Sayfa Ayarları  
Menü: Yönetim > Randevu Kuralları

Sayfa ayarlarında public salon görünümü hazırlanır:

- Logo
- Kapak görseli
- Galeri
- Banner görselleri
- Public sayfada görünecek bölümler

Randevu kurallarında:

- Depozito zorunluluğu
- Ücretsiz iptal süresi
- Geç iptal ücreti
- No-show ücreti
- Kara liste eşiği

ayarlanır.

## 3. Günlük Kullanım

### 3.1 Dashboard

Menü: Salon > Dashboard

Dashboard günün özetidir:

- Toplam müşteri
- Bugünün randevuları
- Bugünün cirosu
- Aktif personel
- Abonelik ve modül durumu
- Kritik stok
- Hatırlatmalar

Güne başlarken ilk bakılacak ekrandır.

### 3.2 Randevu Oluşturma ve Takip

Menü: Salon > Randevular

Randevu oluştururken temel sıra:

1. Müşteri seçin veya yeni müşteri oluşturun.
2. Hizmet seçin.
3. Personel seçin.
4. Tarih ve saat seçin.
5. Not veya özel bilgi varsa ekleyin.
6. Randevuyu kaydedin.

Randevu takibinde durum bilgilerini güncel tutun:

- Bekliyor
- Onaylandı
- Geldi
- Tamamlandı
- İptal
- Gelmedi

Randevu tamamlandığında adisyon/satış akışıyla tahsilat kapatılmalıdır.

### 3.3 Hızlı Satış

Menü: Hızlı Satış

Hızlı Satış günlük POS ekranıdır. Hizmet, ürün, paket, hediye kartı ve üyelik satışlarını tek akışta toparlar.

Önerilen satış akışı:

1. Müşteri seçin veya müşteri olmadan satış yapın.
2. Sepete hizmet/ürün/paket ekleyin.
3. Personel seçimi gerekiyorsa doğru personeli bağlayın.
4. İndirim veya kampanya varsa uygulayın.
5. Ödeme yöntemini seçin.
6. Satışı tamamlayın.

Satış tamamlandığında adisyon, kasa ve varsa stok/seans/puan hareketleri oluşur.

### 3.4 Müşteri Yönetimi

Menü: Salon > Müşteriler

Müşteri kartı salonun hafızasıdır.

Müşteri kartında takip edilebilen bilgiler:

- İletişim bilgileri
- Doğum tarihi ve notlar
- Randevu geçmişi
- Satış/adisyon geçmişi
- Sağlık ve alerji bilgileri
- Tedavi kayıtları
- Reçete/formül kayıtları
- Önce/sonra görselleri
- Onay formları
- Paket, üyelik, puan ve hediye kartı bilgileri

Müşteri detayını düzenli tutmak, tekrar satış ve doğru hizmet sunumu için kritiktir.

### 3.5 Bekleme Listesi

Menü: Salon > Bekleme Listesi

Uygun randevu saati yoksa müşteri bekleme listesine alınır.

Bekleme listesi kaydında:

- Müşteri
- Hizmet
- Tercih edilen tarih/saat aralığı
- Personel tercihi
- Not

tutulur. Boşluk oluştuğunda uygun müşteriye dönüş yapılır.

## 4. Operasyon Yönetimi

### 4.1 Hizmet, Kaynak ve Combo Yönetimi

Menü: Operasyon > Hizmetler

Bu ekran yalnızca fiyat listesi değildir. Aynı zamanda operasyon kapasitesini tanımlar.

Kullanım alanları:

- Hizmet kategorisi oluşturma
- Hizmet fiyatı ve süresi belirleme
- Hizmete oda/cihaz/kaynak bağlama
- Hizmet varyantı veya ek hizmet tanımlama
- Combo hizmet oluşturma
- Seanslı hizmet tanımı yapma

Yanlış süre veya kaynak tanımı randevu kapasitesini etkileyebilir; canlı kullanımdan önce sık verilen hizmetleri test edin.

### 4.2 Personel Fiyatları ve Hasılat Paylaşımı

Menü: Operasyon > Personel Fiyatları

Aynı hizmet farklı personelde farklı fiyatla satılıyorsa personel bazlı fiyat kullanılır.

Hasılat paylaşımı/komisyon için personel, hizmet veya ürün bazında kural oluşturulabilir. Bu bilgiler personel raporları ve bordro hazırlığı için temel veri sağlar.

### 4.3 Reçeteler

Menü: Operasyon > Reçeteler

Reçeteler hizmet sırasında kullanılan ürünleri tanımlar.

Örnek:

- Saç boyama hizmeti
- Boya, oksidan, bakım ürünü
- Her üründen kullanılacak miktar

Reçete doğru tanımlanırsa satış/randevu sonrası stok tüketimi daha sağlıklı takip edilir.

## 5. Stok ve Tedarik

### 5.1 Ürünler

Menü: Stok > Ürünler

Ürün kartında:

- Ürün adı
- Barkod
- Kategori
- Marka
- Alış/satış fiyatı
- KDV
- Kritik stok
- Şube bazlı stok

bilgileri tutulur.

Çok şubeli kullanımda stok şube bazlıdır. Salon sahibi toplamı ve şubeleri görebilir; şube kullanıcısı kendi şubesinin stok durumuyla çalışır.

### 5.2 Tedarikçiler

Menü: Stok > Tedarikçiler

Tedarikçi kartları satın alma ve cari takip için kullanılır.

Takip edilebilen bilgiler:

- Tedarikçi iletişim bilgileri
- Siparişler
- Borç/alacak hareketleri
- Ürün tedarik ilişkileri

## 6. Finans

### 6.1 Adisyonlar

Menü: Finans > Adisyonlar

Adisyon, satışın detay kaydıdır. Hizmet, ürün, paket, ödeme, iade ve iptal bilgilerini bir arada gösterir.

Adisyon ekranında:

- Yeni adisyon oluşturulur.
- Mevcut adisyon detayı görüntülenir.
- Ödeme durumu takip edilir.
- İade yapılır.
- Açık adisyon iptal edilir.

### 6.2 Kasa

Menü: Finans > Kasa

Kasa ekranı nakit ve ödeme hareketlerini takip eder.

Günlük kullanım:

1. Gün başında kasa bakiyesini kontrol edin.
2. Satışlar otomatik kasa hareketi oluşturur.
3. Manuel giriş/çıkış gerekiyorsa açıklama ile kaydedin.
4. Gün sonunda kasa kapanışı yapın.

### 6.3 Masraflar

Menü: Finans > Masraflar

Kira, temizlik, ürün alımı, personel avansı gibi giderler bu ekrana girilir.

Masraf kaydında kategori, tutar, tarih, şube ve açıklama alanlarını doldurun. Raporlarda karlılık ve gider kırılımı için bu veri kullanılır.

### 6.4 Hediye Kartları

Menü: Finans > Hediye Kartları

Hediye kartı oluştururken:

- Tutar
- Son kullanma tarihi
- Alıcı adı/telefonu
- Gönderen adı
- Mesaj

girilir. Kart kullanıldıkça kalan bakiye takip edilir.

### 6.5 Seans Paketleri

Menü: Finans > Seans Paketleri

Seans paketleri müşteriye satılan hizmet hakkını takip eder.

Paketlerde dikkat:

- Paket tanımı hizmete bağlı olmalıdır.
- Satış sonrası müşterinin kalan seansı takip edilir.
- Seans kullanımı randevu, adisyon veya manuel kullanım kaydıyla düşülür.

## 7. Pazarlama ve Müşteri Sadakati

### 7.1 Kampanyalar

Menü: Pazarlama > Kampanyalar

Kampanyalar müşteri segmentlerine göre hazırlanır.

Örnek segmentler:

- Uzun süredir gelmeyen müşteriler
- Doğum günü yaklaşan müşteriler
- Belirli hizmeti alan müşteriler
- Belirli şubeye bağlı müşteriler

### 7.2 Üyelik Planları

Menü: Pazarlama > Üyelik Planları

Üyelik planı, müşteriye belirli dönem veya avantaj seti tanımlar.

Kullanım örnekleri:

- Aylık bakım üyeliği
- VIP indirim üyeliği
- Belirli hizmet grubunda avantaj

### 7.3 Sadakat Programı

Menü: Pazarlama > Sadakat Programı

Sadakat programı puan kazanma ve kullanma kurallarını yönetir.

Örnek:

- Her 100 TL harcamaya 5 puan
- 1 puan = 1 TL indirim
- Minimum kullanım eşiği

### 7.4 E-posta Kampanyaları

Menü: Pazarlama > E-posta Kampanyaları

E-posta kampanyası için önce e-posta ayarlarının tamamlanmış olması gerekir.

Kampanya oluştururken:

- Konu
- İçerik
- Şube hedefi
- Segment filtresi
- Planlanan gönderim tarihi

belirlenir.

### 7.5 Yorumlar

Menü: Pazarlama > Yorumlar

Yorumlar müşteri memnuniyeti takibi için kullanılır. Randevu veya satış sonrası yorum isteme akışlarıyla birlikte değerlendirilmelidir.

### 7.6 Geri Kazanım

Menü: Pazarlama > Geri Kazanım

Uzun süre gelmeyen müşterilere yönelik geri kazanım kuralları oluşturulur.

Örnek:

- 45 gündür randevu almayan müşteriyi listele
- Belirli hizmet grubunda pasifleşen müşteriyi hedefle
- Kampanya veya mesaj akışıyla geri çağır

## 8. Yönetim Ekranları

### 8.1 Onay Formları

Menü: Yönetim > Onay Formları

KVKK, işlem onayı, üyelik sözleşmesi veya hizmete özel rıza metinleri bu ekranda hazırlanır.

Müşteri kartında müşterinin imzaladığı/onayladığı formlar takip edilir.

### 8.2 Önce/Sonra

Menü: Yönetim > Önce/Sonra

Önce/sonra görselleri müşteri, hizmet ve şube ile ilişkilendirilir. Public paylaşım yapılacaksa müşteri onayı ve KVKK hassasiyeti dikkate alınmalıdır.

### 8.3 E-posta Ayarları

Menü: Yönetim > E-posta Ayarları

Salonun kendi e-posta gönderim hesabı bağlanır. Gmail OAuth veya SMTP bilgileri kullanılabilir.

E-posta kampanyası, randevu bildirimi ve müşteri iletişimleri bu ayarlara bağlıdır.

### 8.4 Modüller

Menü: Yönetim > Modüller

Aktif paketler, satın alınabilir modüller, aylık toplam, tahakkuk ödemeleri ve ödeme geçmişi bu ekranda görünür.

Modül satın alma akışında sistem önce kıst dönem tutarını hesaplar, sonra ödeme ekranına yönlendirir. Ödeme sonrası yeni modülün menüde görünmesi için oturum yenileme gerekebilir.

## 9. Raporlar

Menü: Raporlar > Raporlar

Raporlar salon performansını takip etmek için kullanılır.

Başlıca rapor alanları:

- Satış raporu
- KPI raporu
- Personel performansı
- Stok raporu
- Finans raporu
- Müşteri raporu
- Şube karşılaştırma

Raporların doğru çalışması için randevu, adisyon, masraf, stok ve personel verilerinin günlük girilmesi gerekir.

## 10. Müşteri Tarafı

### 10.1 Public Salon Sayfası

Adres örneği: `/salon/{sube-slug}`

Public sayfada müşteri:

- Salon/şube bilgilerini görür.
- Hizmetleri inceler.
- Personel seçebilir.
- Randevu alabilir.
- Uygunsa online ödeme/depozito ödeyebilir.
- Üyelik veya kampanya bilgilerini görebilir.

### 10.2 Online Randevu

Adres örneği: `/salon/{sube-slug}/book`

Müşteri randevu alırken:

1. Hizmet seçer.
2. Personel veya fark etmez seçer.
3. Tarih ve saat seçer.
4. Giriş yapar veya kayıt olur.
5. Depozito gerekiyorsa ödeme yapar.
6. Randevu oluşturulur.

### 10.3 Müşteri Paneli

Adres: `/user/panel`

Müşteri panelinde:

- Randevular
- Ödemeler
- Paket/üyelik hakları
- Yorumlar
- Profil bilgileri

takip edilir.

## 11. Rol ve Şube Kuralları

| Rol | Genel davranış |
| --- | --- |
| Salon sahibi | Tüm şubeleri, yönetim ekranlarını, modülleri ve finansal görünümü yönetir. |
| Şube yöneticisi | Kendi şubesinin operasyonel verisini görür ve yönetir. |
| Personel | Kendi operasyon akışına uygun randevu, müşteri ve satış ekranlarını kullanır. |
| Public müşteri | Public sayfa ve müşteri paneli üzerinden randevu/ödeme/yorum akışlarını kullanır. |

Şube bazlı ekranlarda yanlış şube seçimi canlı operasyonu etkileyebilir. Yeni kayıt açarken şube bilgisini kontrol edin.

## 12. Sık Sorulan Sorular

### Personel randevu ekranında görünmüyor

Personel aktif mi, doğru şubeye bağlı mı, ilgili hizmet yetkinliği seçilmiş mi ve public görünürlük ayarı açık mı kontrol edin.

### Müşteri online randevu alamıyor

Şube public mi, çalışma saatleri dolu mu, hizmet aktif mi, personel müsait mi ve randevu kurallarında depozito/iptal ayarı doğru mu kontrol edin.

### Yeni modül satın aldım ama menüde görünmüyor

Ödeme başarılıysa oturumu yenileyin. Modüller ekranında ödeme sonucu ve aktif paketler kontrol edilmelidir.

### Kasa veya stok yanlış şubede görünüyor

Kullanıcı rolünü, branch selector değerini ve kaydın bağlı olduğu şubeyi kontrol edin.

### Satış sonrası stok düşmedi

Ürün kartında stok var mı, satış doğru şubeden mi yapıldı, hizmet reçetesi doğru ürünlere bağlı mı kontrol edin.

### E-posta kampanyası gitmiyor

E-posta ayarları tamam mı, gönderici hesabı bağlı mı, segmentte e-posta adresi olan müşteri var mı ve kampanya durumu doğru mu kontrol edin.

## 13. Canlı Kullanım Öncesi Kontrol Listesi

- Salon profili dolduruldu.
- En az bir şube oluşturuldu ve public bilgileri tamamlandı.
- Hizmet kategorileri ve hizmetler oluşturuldu.
- Personel hesapları, şube ve hizmet yetkinlikleri tanımlandı.
- Kasa oluşturuldu.
- Ödeme bilgileri kontrol edildi.
- Public sayfa görselleri ve bölümleri ayarlandı.
- Randevu kuralları belirlendi.
- En az bir test müşterisi oluşturuldu.
- Test randevusu oluşturuldu.
- Test satış/adisyon yapıldı.
- Kasa ve stok hareketleri kontrol edildi.
- Müşteri public booking akışı test edildi.
- Modüller ve abonelik durumu kontrol edildi.

## 14. 30 Dakikalık Eğitim Akışı

1. Giriş ve menü mantığı: 3 dk
2. Şube, hizmet, personel ilişkisi: 5 dk
3. Randevu oluşturma ve tamamlama: 5 dk
4. Hızlı satış ve adisyon: 5 dk
5. Müşteri kartı ve tedavi bilgileri: 4 dk
6. Kasa, masraf ve gün sonu: 4 dk
7. Public randevu ve müşteri paneli: 3 dk
8. Sorular ve canlı kullanım kontrol listesi: 1 dk


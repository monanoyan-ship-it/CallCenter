# BTK Yasal On Inceleme Raporu
## VoIP / SIP Trunk Reseller Modeli - Hukuk Danismani Icin

**Tarih:** 2026-03-07
**Hazirlayan:** Teknik Ekip (On Arastirma)
**Amac:** Hukukcuya iletilmek uzere on bilgi toplama

---

## 1. Is Modeli Ozeti

Platformumuz (CorpLynk Call Center), musterilerine cagri merkezi yazilimi sunmaktadir. Mevcut durumda musteriler kendi SIP hesaplarini/trunk'larini getirmek zorundadir.

**Yeni model:** Platformun kendisi toptan SIP trunk/DID satin alip musterilere sunmasi. Musterinin SIP saglayici ile muhatap olmasina gerek kalmaz. Maliyet + kar marji musteriye faturalanir.

**Onemli Not:** Platformumuz ses kaydi tutmayacaktir. Cagri meta verileri (kim, kimi, ne zaman, ne kadar sure aradi) tutulacak, ancak ses icerigi kaydedilmeyecektir.

---

## 2. BTK Yetkilendirme Rejimi

5809 sayili Elektronik Haberlesme Kanunu'na gore iki tur yetkilendirme vardir:

### 2.1. Bildirim
- Numara, frekans gibi kaynak tahsisine **ihtiyac duyulmayan** hizmetler icin
- BTK'ya bildirimde bulunmak yeterli
- Daha hafif yukumlulukler

### 2.2. Kullanim Hakki
- Numara tahsisi gerektiren hizmetler icin
- BTK'dan **kullanim hakki** alinmasi zorunlu
- Daha agir yukumlulukler (altyapi, sermaye, raporlama)

### VoIP Ses Hizmeti Hangi Kategoride?
- VoIP uzerinden **numara tahsisli ses hizmeti** sunulmasi → **Kullanim Hakki** (STH lisansi) gerektirir
- Eger sadece yazilim sunulup musteri kendi numarasini kullaniyorsa → **Bildirim** yeterli olabilir
- **Reseller modeli** (bizim numaramizi musteriye vermek) → STH veya en azindan bildirim gerektirmesi kuvvetle muhtemel

**HUKUKCUYA SORU 1:** Biz numara tahsisi yapmadan, sadece SIP trunk baglantisini musteriye proxy olarak sunarsak (numaralar SIP saglayiciya ait kalir), bu durumda STH lisansi gerekir mi yoksa bildirim yeterli mi?

---

## 3. STH (Sabit Telefon Hizmeti) Lisansi Gereksinimleri

Arastirmamiza gore STH lisansi icin:

### 3.1. Sirket Sartlari
- **Anonim sirket** statusu (Limited sirket de kabul ediliyor)
- Hisselerin **nama yazili** olmasi (A.S. icin)
- Ticaret Sicil Gazetesi'nde **"elektronik haberlesme hizmeti sunulmasi"** faaliyet konusu
- Ortaklarin/yoneticilerin belirli suclardan hukum giymemis olmasi
- **Asgari odenmis sermaye: 1.000.000 TL**

### 3.2. Basvuru Sureci
- CEVHER Sistemi uzerinden (kurumsal.btk.gov.tr)
- E-devlet sifresiyle giris
- Bildirim veya Kullanim Hakki Basvuru Formu doldurma
- Noter onayli imza sirkuleri
- Adli sicil kayitlari

### 3.3. Yillik Mali Yukumlulukler
- Brut cironun **%0.35**'i → Ulastirma ve Altyapi Bakanligi'na
- Brut cironun **%1**'i → BTK'ya (idari ucret)
- Minimum idari ucret: ~114.600 TL/yil (2025 rakami)
- Vergiler: %20 KDV, %15 gelir vergisi, %15 OIV

### 3.4. Altyapi Gereksinimleri
- Teknik personel
- 7/24 destek ekibi (cagri merkezi yazilimi oldugu icin zaten mevcut)
- Sunucu altyapisi (Cloud Run uzerinde zaten mevcut)

**HUKUKCUYA SORU 2:** 1.000.000 TL asgari sermaye zorunlulugu guncel mi? Bu sarti karsilayamiyorsak alternatif yollar var mi (ornegin lisansli bir operatorle is ortakligi)?

---

## 4. Alternatif Yaklasimlar

### Model A: Dogrudan STH Lisansi
- Tam kontrol, kendi numaralarimiz
- Yuksek baslangic maliyeti (sermaye + altyapi)
- BTK denetimi ve raporlama yukumlulugu

### Model B: Lisansli Operatorle Bayilik/Is Ortakligi
- Netgsm, Voip Telekom, Karel gibi STH lisansli bir operatorle anlasma
- Onlarin lisansi altinda hizmet sunulmasi
- Daha dusuk maliyet, daha az burokrasi
- Kontrol ve kar marji daha kisitli

### Model C: Sadece Yazilim + Entegrasyon (Mevcut Model)
- Lisans gerektirmez (sadece yazilim satisi)
- Musteri kendi SIP saglayicisini getirir
- En dusuk risk, en az burokrasi
- Musteri icin daha zor onboarding

### Model D: Hibrit (Onerilen)
- Temel: Yazilim platformu (lisans gerektirmez)
- Opsiyonel: Lisansli operatorle anlasma uzerinden "kolay SIP" secenegi
- Musteri isterse kendi trunk'ini getirir, istemezse biz saglariz
- Risk/maliyet dengesi en iyi

**HUKUKCUYA SORU 3:** Model D (hibrit) yaklasimi icin hangi hukuki yapilandirma en uygun? Bayilik mi, is ortakligi mi, yoksa sadece yonlendirme (referral) mi?

---

## 5. Ses Kaydi Tutmama Karari - Hukuki Degerlendirme

### 5.1. Mevcut Durum
Platformumuz ses kaydi tutmayacaktir. Sadece cagri meta verileri (CDR):
- Arayan/aranan numara
- Cagri baslangic/bitis zamani
- Cagri suresi
- Cagri durumu (cevaplandi, mesgul, cevaplanmadi)

### 5.2. Yasal Cerceve
- **KVKK:** Ses kaydi kisisel veri niteligi tasir. Tutmamak KVKK acisindan daha az risk demektir.
- **TTK:** Ticaret Kanunu bazi sektorlerde ses kaydini zorunlu kilabilir (ornegin SPK duzenlemelerine tabi finansal islemler).
- **BTK:** Elektronik haberlesme isletmecilerinin iletisim verilerini belirli surelerle saklamasi gerekebilir.
- **TCK 132:** Haberlesmein gizliligini ihlal sucu - kayit yapilmamasi bu riski ortadan kaldirir.

### 5.3. Musterinin Kendi Insiyatifi
Musteri kendi sisteminde ses kaydi tutmak isteyebilir - bu musterinin sorumlulugundadir.

**HUKUKCUYA SORU 4:** BTK yetkilendirilmis isletmeci olarak cagri icerigini (ses) kaydetmeme hakki var mi? Yoksa BTK isletmecilerden belirli surelerle ses/iletisim verisi saklamasini zorunlu kilar mi? (5651 sayili kanun ve ilgili yonetmelikler cercevesinde)

**HUKUKCUYA SORU 5:** Musterilerimiz ses kaydi tutmak isterse, KVKK kapsaminda bizim (platform olarak) ve musterinin (veri sorumlusu olarak) sorumluluk dagiliimi nasil olmali? Veri isleyen mi yoksa veri sorumlusu mu oluruz?

---

## 6. OTT Duzenleme Tasarisi (Yakinda Beklenen)

BTK, internet uzerinden haberlesme hizmeti saglayicilari (OTT) icin yeni duzenleme hazirligi icindedir:
- Turkiye'de **yetkili temsilci/sirket** kurma zorunlulugu
- BTK'ya **kullanici istatistikleri raporlama** (aktif kullanici, ses, mesaj)
- Uyumsuzluk cezalari: **1-30 milyon TL** idari para cezasi
- Bant genisligi kisitlama yetkisi (%95'e kadar)
- Gecis suresi yok - yururluge girer girmez uyum zorunlu

**HUKUKCUYA SORU 6:** Bu OTT duzenleme tasarisi bizim platformumuzu da kapsayacak mi? "Kisiden kisiye elektronik haberlesme" tanimi altina girebilir miyiz?

---

## 7. Sonraki Adimlar (Hukukcudan Beklenenler)

1. Yukaridaki 6 sorunun yanitlanmasi
2. En uygun is modeli icin hukuki tavsiye (Model A/B/C/D)
3. BTK bildirim/lisans basvurusu icin gerekli belge listesi
4. Tahmini maliyet ve zaman cizelgesi
5. OTT duzenleme tasarisinin potansiyel etkisi
6. Ses kaydi tutmama karari ile ilgili hukuki risk degerlendirmesi

---

## 8. Referans Kaynaklar

- BTK Yetkilendirme: https://www.btk.gov.tr/yetkilendirme
- BTK Yetkilendirme Rejimi: https://www.btk.gov.tr/elektronik-haberlesme-yetkilendirme-rejimi
- BTK Basvuru Adimlari: https://www.btk.gov.tr/yetkilendirme-icin-basvuru-adimlari
- BTK Yetkilendirmeye Tabi Hizmetler: https://www.btk.gov.tr/yetkilendirmeye-tabi-hizmetler
- Idari Ucret: https://www.btk.gov.tr/idari-ucret
- Kullanim Hakki Ucretleri: https://www.btk.gov.tr/kullanim-hakki-ucretleri
- 5809 Sayili Elektronik Haberlesme Kanunu: https://www.mevzuat.gov.tr
- OTT Duzenleme Haberi: https://gun.av.tr/tr/goruslerimiz/guncel-yazilar/internet-uzerinden-haberlesme-hizmeti-saglayicilari-icin-yeni-duzenlemeler-bekleniyor
- KVKK Cagri Merkezi Karari: https://www.kvkk.gov.tr/Icerik/6932/2020-504

---

*Bu belge on arastirma niteligindedir ve hukuki tavsiye icermez. Nihai karar hukuk danismaninin degerlendirmesine gore verilmelidir.*

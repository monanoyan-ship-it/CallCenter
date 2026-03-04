# Ses Kayitlarini Bulut Depolamaya Yukleme - Kullanici/Admin Kilavuzu

## Genel Bakis

Ses kayitlari Windows uygulamasi tarafindan otomatik olarak musterinin bulut depolama alanina yuklenir. Sunucuya dosya gonderilmez — Windows app dogrudan S3, Google Drive, OneDrive veya Yandex Disk'e yukler.

### Desteklenen Saglayicilar

| Saglayici | Gerekli Bilgiler |
|-----------|-----------------|
| Amazon S3 | Access Key, Secret Key, Bucket Adi, Region |
| MinIO (S3-uyumlu) | Endpoint, Access Key, Secret Key, Bucket Adi |
| Google Drive | Client ID, Client Secret, Refresh Token |
| Microsoft OneDrive | Client ID, Client Secret, Tenant ID, Drive ID |
| Yandex Disk | OAuth Token |

---

## Admin: Bulut Depolamayi Yapilandirma

### 1. Storage Config Olusturma

Admin panelden **Cloud Storage** sayfasina gidin (Web uygulamasi).

**API ile (alternatif):**
```
POST /api/cloud-storage/configs
```

Ornek (Amazon S3):
```json
{
  "customerId": 1,
  "providerTypeId": 4,
  "basePath": "recordings/",
  "isDefault": true,
  "accessKey": "AKIAIOSFODNN7EXAMPLE",
  "secretKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
  "bucketName": "callcenter-recordings",
  "region": "eu-central-1"
}
```

Provider Tip ID'leri:
- 1 = Google Drive
- 2 = OneDrive
- 3 = Yandex Disk
- 4 = Amazon S3
- 5 = MinIO

### 2. Baglanti Testi

Config olusturulduktan sonra baglantiyi test edin:

```
POST /api/cloud-storage/configs/{configId}/test
```

Basarili sonuc:
```json
{
  "success": true,
  "error": null,
  "testedAt": "2026-03-04T12:00:00Z"
}
```

### 3. Varsayilan Config

Her musterinin bir **varsayilan (default)** storage config'i olmalidir. Yeni config olusturulurken `isDefault: true` ayarlanirsa, mevcut default otomatik kaldirilir.

---

## Nasil Calisir?

### Otomatik Yukleme Akisi

```
1. Agent arama yapar ve cagri biter
2. Ses kaydi sifrelenerek .enc dosyasi olusturulur (AES-256)
3. 60 saniye sonra BackgroundSync devreye girer:
   a. Cagri metadata'si API'ye push edilir
   b. Cloud config kontrol edilir (var mi?)
   c. .enc dosyasi dogrudan bulut'a yuklenir
   d. Basarili → lokal dosya silinir, CloudFileId kaydedilir
   e. CloudFileId sonraki sync'te API'ye iletilir
```

### Agent/Kullanici Tarafinda Yapilacak Bir Sey Yok

Yukleme tamamen otomatiktir. Windows uygulamasi:
- Baglanti varsa cloud config'i API'den alir ve cache'ler
- Baglanti kesilirse cache'teki config ile yuklemeye devam eder
- Basarisiz yuklemeler sonraki turda tekrar denenir (en fazla 5 deneme)

### Ses Kaydi Dosyasi Nerede?

| Durum | Dosya Yeri |
|-------|-----------|
| Cloud config YOK | Lokal diskte kalir (silinmez) |
| Cloud config VAR, yukleme bekliyor | Lokal diskte (60sn icerisinde yuklenecek) |
| Yukleme basarili | Lokal silindi, bulut'ta |
| Yukleme basarisiz (5 deneme) | Lokal diskte kalir |

---

## Saglayici Bazinda Yapilandirma Detaylari

### Amazon S3

1. AWS Console'dan IAM kullanicisi olusturun
2. S3 bucket olusturun (region secin)
3. IAM kullanicisina bucket erisimi verin (PutObject, GetObject, DeleteObject)
4. Access Key ve Secret Key'i admin panele girin

**Ornek BasePath:** `recordings/` (bucket icerisindeki klasor)

### MinIO (Self-hosted S3)

1. MinIO sunucunuz kurulu olmalidir
2. Bucket olusturun
3. Access/Secret key olusturun
4. Endpoint'i girin (ornek: `https://minio.example.com:9000`)

> MinIO icin `useSSL: false` ayari HTTP baglantisi icin kullanilabilir (sadece test ortami).

### Google Drive

1. Google Cloud Console'dan proje olusturun
2. Drive API'yi etkinlestirin
3. OAuth 2.0 Client ID olusturun (Desktop uygulamasi)
4. Refresh token alin (OAuth playground veya kendi akisiniz ile)
5. Client ID, Client Secret ve Refresh Token'i admin panele girin

**FolderId (opsiyonel):** Belirli bir klasore yuklemek icin Google Drive klasor ID'sini girin. Bos birakilirsa root'a yuklenir.

### Microsoft OneDrive

1. Azure Portal'dan App Registration olusturun
2. API permissions: `Files.ReadWrite.All` (Application turunde)
3. Client Secret olusturun
4. Tenant ID, Client ID, Client Secret girin
5. **DriveId zorunludur** — Application credential ile calisirken hangi drive'a yuklenecegi belirtilmelidir

**DriveId nasil bulunur:**
```
GET https://graph.microsoft.com/v1.0/drives
```

### Yandex Disk

1. Yandex OAuth uygulamasi olusturun (https://oauth.yandex.ru/)
2. OAuth token alin
3. Token'i admin panele girin

**BasePath (opsiyonel):** Ornek: `/CallCenter/Recordings/` — otomatik olusturulur.

---

## Sorun Giderme

### Yukleme Calismiyorsa

1. **Cloud config var mi?** Admin panelden kontrol edin
2. **Baglanti testi basarili mi?** `POST /api/cloud-storage/configs/{id}/test`
3. **Windows app log'lari:** `%LOCALAPPDATA%\CallCenter\` altinda debug output
4. **Max deneme:** 5 basarisiz denemeden sonra durur. Sorunu duzelttikten sonra deneme sayacini sifirlamak icin uygulamayi yeniden baslatin

### Sik Karsilasilan Hatalar

| Hata | Sebep | Cozum |
|------|-------|-------|
| "Musteri icin depolama yapilandirilmamis" | Cloud config yok veya pasif | Admin panelden config olusturun/aktif edin |
| "S3 baglanti hatasi" | Yanlis credential veya bucket | Access Key, Secret Key ve Bucket adini kontrol edin |
| "Google Drive upload hatasi" | Refresh token suresi dolmus | Yeni refresh token alin |
| "OneDrive DriveId yapilandirilmamis" | DriveId girilmemis | Admin panelden DriveId girin |
| "Yandex upload URL alinamadi" | OAuth token gecersiz | Yeni OAuth token alin |

### Guvenlik Notlari

- Ses kayitlari AES-256 ile sifrelenerek yuklenir (.enc formati)
- Cloud credential'lari API veritabaninda AES-256 ile sifrelenmis saklanir
- Windows uygulamasinda credential'lar DPAPI (Windows SecureStorage) ile korunur
- Credential'lar API'den alinirken HTTPS uzerinden iletilir
- TTK md. 82 uyarinca ses kayitlari 10 yil saklanir

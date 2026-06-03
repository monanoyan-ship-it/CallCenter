# Sistem E-posta Ayarları

Bu not sistemin kendi gönderdiği e-postalar içindir: hesap doğrulama, şifre sıfırlama ve platform bildirimleri. Salon müşterisinin kampanya göndermek için bağladığı Gmail/SMTP hesabı bu akıştan ayrıdır.

## İki Ayrı E-posta Akışı

1. Sistem e-postaları

- API tarafından gönderilir.
- `PlatformEmail:*` config değerlerini kullanır.
- Örnek: kayıt doğrulama, şifre sıfırlama.
- Management > E-posta Şablonları ekranındaki şablonlar bu akışta kullanılır.

2. Salon e-posta entegrasyonu

- Salon müşterisinin kendi Gmail, Office365, Yandex veya SMTP hesabıyla gönderilir.
- Salon > E-posta Ayarları ekranındaki entegrasyonları kullanır.
- Örnek: salon e-posta kampanyaları.

## Sistem E-postası İçin Gerekli Config

Production ve deploy ortamlarında değerler dosyaya yazılmamalı, environment variable olarak verilmelidir.

```text
PlatformEmail__Host=smtp.resend.com
PlatformEmail__Port=587
PlatformEmail__Username=resend
PlatformEmail__Password=<smtp-api-key>
PlatformEmail__FromEmail=info@corplynk.com
PlatformEmail__FromName=CorpLynk
PlatformEmail__UseSsl=false
```

## Önerilen Sağlayıcı

Sistem e-postaları için Gmail yerine Resend, Mailgun, SendGrid benzeri transactional e-posta sağlayıcısı tercih edilmeli. Gmail kişisel posta hesabı gibi davrandığı için limit, spam ve güvenlik kısıtları daha çabuk sorun çıkarır.

Resend SMTP için tipik ayar:

```text
Host=smtp.resend.com
Port=587
Username=resend
Password=<Resend SMTP API key>
FromEmail=info@corplynk.com
FromName=CorpLynk
UseSsl=false
```

## Gmail İle Local/Test Kullanımı

Local test için Gmail kullanılacaksa normal Gmail şifresi kullanılmaz. Google hesabında iki adımlı doğrulama açılıp uygulama şifresi alınmalıdır.

```text
Host=smtp.gmail.com
Port=587
Username=<gmail-adresi>
Password=<16-haneli-uygulama-sifresi>
FromEmail=<gmail-adresi>
FromName=CorpLynk
UseSsl=false
```

Alternatif olarak direkt SSL portu:

```text
Host=smtp.gmail.com
Port=465
Username=<gmail-adresi>
Password=<16-haneli-uygulama-sifresi>
FromEmail=<gmail-adresi>
FromName=CorpLynk
UseSsl=true
```

Google uygulama şifresi ekranda dörderli gruplar halinde görünebilir. Kod Gmail SMTP için boşlukları temizler; yine de config'e boşluksuz yazmak daha temizdir.

## TLS Port Kuralı

SMTP tarafında en sık görülen hata 587 portuna direkt SSL ile bağlanmaktır.

- `465`: SSL-on-connect
- `587`: STARTTLS

Kod generic SMTP için bu ayrımı porttan çözer. Böylece Gmail `587` kaydında `SecureSocketOptions.StartTls`, `465` kaydında `SecureSocketOptions.SslOnConnect` kullanılır.

## Şablon Akışı

Management > E-posta Şablonları ekranında:

- E-posta türü sistemin hangi olayda mail göndereceğini ifade eder.
- Teknik anahtar kod tarafından kullanılır, örnek: `user_password_reset`.
- Şablonlar dil bazlıdır.
- Değişkenler `{{FullName}}`, `{{VerifyUrl}}` gibi yazılır.
- Önizleme gerçek mail göndermeden HTML'i gösterir.
- Test Gönder, sistem e-posta ayarıyla gerçek mail göndermeyi dener.

## Sorun Giderme

- `PlatformEmail:Password yapılandırılmamış`: SMTP şifresi/API key eksik.
- `SSL or TLS connection` ve port 587 uyarısı: 587 STARTTLS ister; 465 direkt SSL ister.
- Gmail authentication failed: uygulama şifresi yanlış, iki adımlı doğrulama kapalı veya Google hesabı uygulama şifresine izin vermiyor olabilir.
- Mail gidiyor ama spam'e düşüyor: domain SPF, DKIM ve DMARC kayıtları kontrol edilmeli.

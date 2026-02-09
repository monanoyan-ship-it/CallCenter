# Call Center Projesi - Notlar

## Kullanıcının İsteği
- MicroSIP (https://www.microsip.org/) benzeri bir uygulamanın incelenmesi
- Kodlarının ve yapısının .NET MAUI'ye aktarılıp aktarılamayacağının araştırılması
- Call center klasörü oluşturuldu, proje buradan devam edecek

## MicroSIP İnceleme Sonucu

### Nedir?
- Windows için açık kaynaklı SIP softphone (yazılımsal telefon)
- VoIP aramaları yapmak için kullanılıyor
- Lisans: GNU GPL v2

### Teknik Detaylar
- **Dil**: C / C++
- **Altyapı**: PJSIP stack
- **Platform**: Sadece Windows (7+)
- **Boyut**: <2.5MB, <5MB RAM
- **Protokoller**: SIP, STUN, ICE
- **Codec'ler**: Opus, G.711, G.722, G.729, GSM, AMR, iLBC, Speex
- **Video**: H.264, H.263+, VP8
- **Güvenlik**: TLS/SRTP şifreleme
- **Diğer**: WebRTC echo cancellation, DTMF, mesajlaşma (SIMPLE), 20+ dil desteği

### MAUI'ye Aktarma Değerlendirmesi
**Sonuç: Direkt aktarım çok zor ve pratik değil.**

Sebepleri:
1. C/C++ ile yazılmış - MAUI ise C#/.NET tabanlı, baştan yazmak gerekir
2. PJSIP stack - Düşük seviye SIP kütüphanesi, MAUI'de karşılığı yok
3. Gerçek zamanlı ses/video işleme - Her platform için native API gerektirir
4. WebRTC/Echo cancellation - Platform bazlı native implementasyon gerektirir

### Alternatif Yaklaşımlar
- SIP.js veya benzeri bir SIP kütüphanesi ile web tabanlı softphone
- MAUI + WebView ile hibrit yaklaşım
- Twilio / Vonage / SignalWire gibi VoIP API servisleri kullanmak
- MAUI'de PJSIP için native binding yazmak (zor ama mümkün)

## Sonraki Adım
Kullanıcıdan beklenen karar: Call center yazılımı mı yapılacak, yoksa sadece SIP telefon özelliği mi eklenecek?

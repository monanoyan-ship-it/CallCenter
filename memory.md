# Call Center Projesi - Hafıza

## Proje Bilgisi
- **Konum**: `C:\Users\Ahmet\source\repos\monanoyan-ship-it\callcenter`
- **Solution**: `CallCenter.slnx` (.NET 10 yeni slnx formatı)
- **Kullanıcı**: .NET uzmanı, ilk call center projesi, Türkçe konuşuyor
- **Takip dosyaları**: `yol_haritasi.xml` (görev takibi), `patterns.md` (geliştirme günlüğü)

## Mevcut Durum
- Faz 1 (Temel Altyapı): TAMAMLANDI
- Faz 2 (Web Arayüzü): SIRADA
- DB: PostgreSQL (localhost:5432, callcenter)
- Admin: admin / admin123

## Önemli Notlar
- .NET 10 kullanılıyor - `dotnet new sln` artık `.slnx` oluşturuyor
- VoIP dinamik olacak (admin panelinden SIP bilgileri girilecek)
- UI bir kere Blazor ile yazılacak, 3 platformda paylaşılacak
- MicroSIP C++ referans: `microsip-reference/` klasöründe
- CORS ayarı: localhost:7100 ve localhost:5100
- Memory dosyaları proje klasöründe tutulacak (memory.md)

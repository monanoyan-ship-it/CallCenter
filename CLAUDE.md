# Call Center Projesi - Claude Talimatları

## Her Oturumda Yapılacak
- Bu dosya okunduktan sonra `patterns.md` ve `yol_haritasi.xml` de okunacak.
- Mevcut durumu anlamadan kod yazılmayacak.

## Kesin Kurallar
1. **COMMIT YOK**: Kullanıcı açıkça "commit et" demeden asla git commit yapılmayacak.
2. **RUN/DEBUG YOK**: Asla `dotnet run`, `dotnet watch`, `dotnet test` veya benzeri çalıştırma komutu kullanılmayacak. Kullanıcı Visual Studio'da debug modda test edecek.
3. **YALAN YOK**: Eksik varsa eksik, yanlış olma ihtimali varsa ihtimal raporlanacak. Emin değilsen "emin değilim" de.
4. **RİSK RAPORU**: Her tamamlanan görevin altına olası riskler/eksikler `yol_haritasi.xml`'e `<Riskler>` etiketi ile yazılacak.
5. **BUILD KONTROL**: `dotnet build` çalıştırılabilir ama `dotnet run` ASLA.
6. **TÜRKÇE**: Kullanıcıyla Türkçe iletişim kur.

## Proje Bilgisi
- **Solution**: `CallCenter.slnx` (.NET 10, slnx formatı)
- **DB**: PostgreSQL (localhost:5432, callcenter)
- **Admin**: admin / admin123
- **Repo**: https://github.com/monanoyan-ship-it/CallCenter (private)
- **Takip**: `yol_haritasi.xml` (görev/risk takibi), `patterns.md` (kararlar/günlük)

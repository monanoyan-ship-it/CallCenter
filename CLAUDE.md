# Call Center Projesi - Claude Talimatları

## Her Oturumda Yapılacak
- Bu dosya okunduktan sonra **ClaudeManager API**'sinden proje bilgilerini oku:
  - `curl -s http://127.0.0.1:41847/api/projects/15/patterns` → Pattern'ler (kurallar, kararlar, hatalar)
  - `curl -s http://127.0.0.1:41847/api/projects/15/roadmap` → Yol haritası (fazlar, görevler, riskler)
- Mevcut durumu anlamadan kod yazılmayacak.

## Kesin Kurallar
1. **COMMIT YOK**: Kullanıcı açıkça "commit et" demeden asla git commit yapılmayacak.
2. **RUN/DEBUG YOK**: Asla `dotnet run`, `dotnet watch`, `dotnet test` veya benzeri çalıştırma komutu kullanılmayacak. Kullanıcı Visual Studio'da debug modda test edecek.
3. **YALAN YOK**: Eksik varsa eksik, yanlış olma ihtimali varsa ihtimal raporlanacak. Emin değilsen "emin değilim" de.
4. **RİSK RAPORU**: Her tamamlanan görevin altına olası riskler/eksikler ClaudeManager'a kaydedilecek (`PUT /api/tasks/{id}` ile risks alanı güncellenir).
5. **BUILD KONTROL**: `dotnet build` çalıştırılabilir ama `dotnet run` ASLA.
6. **TÜRKÇE**: Kullanıcıyla Türkçe iletişim kur.

## Proje Bilgisi
- **Solution**: `CallCenter.slnx` (.NET 10, slnx formatı)
- **DB**: PostgreSQL (localhost:5432, callcenter)
- **Admin**: admin / admin123
- **Repo**: https://github.com/monanoyan-ship-it/CallCenter (private)
- **Takip**: ClaudeManager API (http://127.0.0.1:41847, project_id: 15)
  - Pattern'ler: `GET /api/projects/15/patterns` (kurallar, kararlar, hatalar, tercihler)
  - Yol Haritası: `GET /api/projects/15/roadmap` (fazlar, görevler, riskler)
  - Yeni pattern: `POST /api/patterns` (project_id, type, title, description)
  - Görev güncelle: `PUT /api/tasks/{id}` (status, risks)

# Call Center Projesi - Claude Talimatları

## Her Oturumda Yapılacak
- **ClaudeManager API**'sinden proje bilgilerini oku:
  - `curl -s http://127.0.0.1:41847/api/projects/15/patterns` → Kurallar, kararlar, hatalar, tercihler
  - `curl -s http://127.0.0.1:41847/api/projects/15/roadmap` → Fazlar, görevler, riskler
- Mevcut durumu anlamadan kod yazılmayacak.

## Proje Bilgisi
- **Solution**: `CallCenter.slnx` (.NET 10, slnx formatı)
- **DB**: PostgreSQL (localhost:5432, callcenter)
- **Admin**: admin / admin123
- **Repo**: https://github.com/monanoyan-ship-it/CallCenter (private)
- **Takip**: ClaudeManager API (http://127.0.0.1:41847, project_id: 15)
  - Pattern CRUD: `POST/PUT/DELETE /api/patterns`
  - Görev güncelle: `PUT /api/tasks/{id}` (status, risks)
  - Faz oluştur: `POST /api/projects/15/phases`
  - Görev oluştur: `POST /api/phases/{id}/tasks`

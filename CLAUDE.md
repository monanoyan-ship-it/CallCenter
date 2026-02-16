# Call Center Projesi - Claude Talimatları

## Her Oturumda Yapılacak
- **ClaudeManager rehberini oku** (tüm kurallar, hatalar, tercihler, yol haritası tek seferde):
  - `curl -s "http://127.0.0.1:41847/api/guide?cwd=c:/Users/Ahmet/source/repos/monanoyan-ship-it/callcenter"`
- Mevcut durumu anlamadan kod yazılmayacak.

## Proje Bilgisi
- **Solution**: `CallCenter.slnx` (.NET 10, slnx formatı)
- **DB**: PostgreSQL (localhost:5432, callcenter)
- **Admin**: admin / admin123
- **Repo**: https://github.com/monanoyan-ship-it/CallCenter (private)

## ClaudeManager API (http://127.0.0.1:41847, project_id: 15)

### Okuma
- Rehber (tek seferde her şey): `GET /api/guide?cwd=PROJE_YOLU`
- Pattern'ler: `GET /api/projects/15/patterns`
- Yol haritası: `GET /api/projects/15/roadmap`
- Notlar (hesap bilgileri, key'ler, config): `GET /api/projects/15/notes`
- Günlük: `GET /api/projects/15/journal`
- Arama: `GET /api/search?q=TERIM&project=15`
- Analitik: `GET /api/projects/15/analytics`

### Yazma
- Pattern (sadece rule|mistake|preference): `POST /api/patterns` + `PUT/DELETE /api/patterns/ID`
- Not: `POST /api/projects/15/notes` + `PUT/DELETE /api/notes/ID`
- Günlük: `POST /api/projects/15/journal` + `PUT/DELETE /api/journal/ID`
- Görev ekle: `POST /api/phases/FAZ_ID/tasks`
- Görev güncelle: `PUT /api/tasks/GOREV_ID`

### Ne Nereye Yazılır
- **rule/mistake/preference** → Pattern (kalıcı kurallar, hatalar, tercihler)
- **Hesap bilgileri, API key, şifre, config** → Notes (category: teknik)
- **Günlük nitelikli bilgi** (kredi, domain, vize, deploy) → Journal

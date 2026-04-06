# Call Center Projesi - Claude Talimatları

## KESİN EMİRLER

### 1. ClaudeManager ZORUNLU
Her oturumun İLK işi ClaudeManager rehberini okumaktır. Rehber okunmadan KOD YAZILMAZ.
**project_id: 15** — Tüm endpoint'lerde bu ID kullanılır. `?cwd=` KULLANMA.
```
curl -s http://127.0.0.1:41847/api/projects/15/patterns
curl -s http://127.0.0.1:41847/api/projects/15/notes
curl -s http://127.0.0.1:41847/api/projects/15/journal
curl -s http://127.0.0.1:41847/api/projects/15/roadmap/summary
```
Rehberdeki tüm kurallar, hatalar ve tercihler bu oturumda GEÇERLİDİR.

### 2. ClaudeManager'a Yazma ZORUNLU
- Yeni kural/hata/tercih öğrenildiğinde → **Pattern** olarak kaydet
- Yeni hesap/API key/şifre oluşturulduğunda → **Notes'a** hemen yaz
- Günlük bilgi (kredi, domain, deploy vb.) → **Journal'a** yaz
- Görev tamamlandığında → **Risk raporu** görev kaydına ekle

### 3. Rehber Okunamazsa
ClaudeManager'a erişilemiyorsa kullanıcıyı bilgilendir ve onay almadan devam etme.

### 4. DEPLOY BİLGİLERİ ClaudeManager'da
Deploy bilgileri (Cloud Run servis adları, domain'ler, Dockerfile konumları, env var'lar, credential'lar) **ClaudeManager Notes**'ta saklanır. Deploy yapmadan önce **mutlaka** ilgili notları oku:
```
curl -s http://127.0.0.1:41847/api/projects/15/notes
```
Önemli notlar: #52 (API Deploy), #114 (Salon Deploy), #58 (Windows App).

**Dockerfile KURALI:** Root Dockerfile her zaman API'ye ait olmalı. Salon/CRM/Management deploy ederken:
1. `cp Dockerfile Dockerfile.api.bak`
2. `cp src/CallCenter.{Proje}/Dockerfile Dockerfile`
3. Deploy et
4. `mv Dockerfile.api.bak Dockerfile` ← **UNUTMA!**
5. `head -3 Dockerfile` ile doğrula (CallCenter API yazmalı)

---

## ClaudeManager v2.0 Kullanım Kılavuzu

**Base URL:** `http://127.0.0.1:41847` | **project_id:** `15` | **Version:** `2.0.0`

### Okuma
| Ne | Endpoint |
|----|----------|
| Rehber (her şey tek seferde) | `GET /api/guide?cwd=PROJE_YOLU` |
| Kurallar/hatalar/tercihler | `GET /api/projects/15/patterns` |
| Yol haritası | `GET /api/projects/15/roadmap` |
| Yol haritası özet | `GET /api/projects/15/roadmap/summary` |
| Yol haritası istatistik | `GET /api/projects/15/roadmap/stats` |
| Notlar (hesap, key, config) | `GET /api/projects/15/notes` |
| Günlük | `GET /api/projects/15/journal` |
| Session'lar | `GET /api/projects/15/sessions?page=1&limit=20` |
| Prompt geçmişi | `GET /api/projects/15/prompts?page=1&limit=10` |
| Tool kullanımları | `GET /api/projects/15/tool-uses?page=1&limit=20` |
| Arama | `GET /api/search?q=TERIM&project=15` |
| Analitik | `GET /api/projects/15/analytics?days=30` |
| Sağlık kontrolü | `GET /health` |
| Proje dışa aktar | `GET /api/projects/15/export` |

### Yazma
| Ne | Endpoint | Tipler |
|----|----------|--------|
| Pattern | `POST /api/patterns` | rule, mistake, preference |
| Pattern güncelle/sil | `PUT/DELETE /api/patterns/ID` | |
| Not | `POST /api/projects/15/notes` | category: teknik, genel, karar, todo |
| Not güncelle/sil | `PUT/DELETE /api/notes/ID` | |
| Not sabitle | `PUT /api/notes/ID` | `{"is_pinned": 1}` |
| Günlük | `POST /api/projects/15/journal` | category: genel, teknik, karar, arastirma |
| Günlük güncelle/sil | `PUT/DELETE /api/journal/ID` | |
| Faz ekle | `POST /api/projects/15/phases` | |
| Faz güncelle/sil | `PUT/DELETE /api/phases/FAZ_ID` | |
| Görev ekle | `POST /api/phases/FAZ_ID/tasks` | |
| Görev güncelle/sil | `PUT/DELETE /api/tasks/GOREV_ID` | |
| Roadmap XML import | `POST /api/projects/15/roadmap/import` | XML body |
| Proje birleştir | `POST /api/projects/merge` | |

### Ne Nereye Yazılır
- **Kalıcı kural** → Pattern (type: rule)
- **Yapılan hata, ders** → Pattern (type: mistake)
- **Kullanıcı tercihi** → Pattern (type: preference)
- **Hesap bilgisi, API key, şifre, config** → Notes (category: teknik)
- **Günlük bilgi** (kredi, domain, vize, deploy) → Journal
- **Görev riski/eksiği** → `PUT /api/tasks/ID` (risks alanı)

### Hooks (Otomatik Takip)
ClaudeManager 4 hook ile oturumları otomatik takip eder:
- **SessionStart**: Oturum başlangıcı kaydı + context injection
- **UserPromptSubmit**: Her prompt kaydı + benzer istek uyarısı
- **PostToolUse** (Edit/Write/Bash): Tool kullanım takibi
- **SessionEnd**: Oturum kapanış kaydı

### Doğru Kullanım Örnekleri

Yeni kural kaydet:
```
curl -X POST http://127.0.0.1:41847/api/patterns -H "Content-Type: application/json" \
  -d '{"project_id":15,"type":"rule","title":"BASLIK","description":"ACIKLAMA"}'
```

Hata kaydet:
```
curl -X POST http://127.0.0.1:41847/api/patterns -H "Content-Type: application/json" \
  -d '{"project_id":15,"type":"mistake","title":"BASLIK","description":"ACIKLAMA"}'
```

Not kaydet (hesap/key/config):
```
curl -X POST http://127.0.0.1:41847/api/projects/15/notes -H "Content-Type: application/json" \
  -d '{"title":"BASLIK","content":"ICERIK","category":"teknik"}'
```

Günlük girişi:
```
curl -X POST http://127.0.0.1:41847/api/projects/15/journal -H "Content-Type: application/json" \
  -d '{"title":"BASLIK","content":"ICERIK","category":"teknik"}'
```

Görev risk raporu:
```
curl -X PUT http://127.0.0.1:41847/api/tasks/GOREV_ID -H "Content-Type: application/json" \
  -d '{"status":"completed","risks":"OLASI RISKLER VE EKSIKLER"}'
```

Faz ekle:
```
curl -X POST http://127.0.0.1:41847/api/projects/15/phases -H "Content-Type: application/json" \
  -d '{"phase_no":"X","title":"FAZ_BASLIGI","sort_order":99}'
```

Not sabitle:
```
curl -X PUT http://127.0.0.1:41847/api/notes/NOT_ID -H "Content-Type: application/json" \
  -d '{"is_pinned":1}'
```

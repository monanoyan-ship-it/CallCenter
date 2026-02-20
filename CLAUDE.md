# Call Center Projesi - Claude Talimatları

## KESİN EMİRLER

### 1. ClaudeManager ZORUNLU
Her oturumun İLK işi ClaudeManager rehberini okumaktır. Rehber okunmadan KOD YAZILMAZ.
```
curl -s "http://127.0.0.1:41847/api/guide?cwd=c:/Users/Ahmet/source/repos/monanoyan-ship-it/callcenter"
```
Rehberdeki tüm kurallar, hatalar ve tercihler bu oturumda GEÇERLİDİR.

### 2. ClaudeManager'a Yazma ZORUNLU
- Yeni kural/hata/tercih öğrenildiğinde → **Pattern** olarak kaydet
- Yeni hesap/API key/şifre oluşturulduğunda → **Notes'a** hemen yaz
- Günlük bilgi (kredi, domain, deploy vb.) → **Journal'a** yaz
- Görev tamamlandığında → **Risk raporu** görev kaydına ekle

### 3. Rehber Okunamazsa
ClaudeManager'a erişilemiyorsa kullanıcıyı bilgilendir ve onay almadan devam etme.

---

## ClaudeManager Kullanım Kılavuzu

**Base URL:** `http://127.0.0.1:41847` | **project_id:** `15`

### Okuma
| Ne | Endpoint |
|----|----------|
| Rehber (her şey tek seferde) | `GET /api/guide?cwd=PROJE_YOLU` |
| Kurallar/hatalar/tercihler | `GET /api/projects/15/patterns` |
| Yol haritası | `GET /api/projects/15/roadmap` |
| Notlar (hesap, key, config) | `GET /api/projects/15/notes` |
| Günlük | `GET /api/projects/15/journal` |
| Arama | `GET /api/search?q=TERIM&project=15` |
| Analitik | `GET /api/projects/15/analytics` |

### Yazma
| Ne | Endpoint | Tipler |
|----|----------|--------|
| Pattern | `POST /api/patterns` | rule, mistake, preference |
| Pattern güncelle/sil | `PUT/DELETE /api/patterns/ID` | |
| Not | `POST /api/projects/15/notes` | category: teknik |
| Not güncelle/sil | `PUT/DELETE /api/notes/ID` | |
| Günlük | `POST /api/projects/15/journal` | category: genel, teknik, karar, arastirma |
| Günlük güncelle/sil | `PUT/DELETE /api/journal/ID` | |
| Görev ekle | `POST /api/phases/FAZ_ID/tasks` | |
| Görev güncelle | `PUT /api/tasks/GOREV_ID` | |

### Ne Nereye Yazılır
- **Kalıcı kural** → Pattern (type: rule)
- **Yapılan hata, ders** → Pattern (type: mistake)
- **Kullanıcı tercihi** → Pattern (type: preference)
- **Hesap bilgisi, API key, şifre, config** → Notes (category: teknik)
- **Günlük bilgi** (kredi, domain, vize, deploy) → Journal
- **Görev riski/eksiği** → `PUT /api/tasks/ID` (risks alanı)

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

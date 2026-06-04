# CorpLynk CallCenter — Sistem / Entegrasyon Test Haritası

Bu doküman tarayıcıdan tıklayarak değil, **HTTP / SQL / SignalR / CLI** seviyesinde çalıştırılan testleri kapsar. UI/click testleri için ayrı doküman: `TEST_MAP.md`.

**Hedef**: Backend kontratını, veri tutarlılığını, async akışları, güvenlik katmanını ve performans bütçesini doğrulamak. Tarayıcı renderlama burada test edilmez.

**Çalıştırma araçları**: `curl`, `psql`, `dotnet test`, Postman/Bruno, k6/locust, SignalR client (JS veya .NET), `gh` CLI.

---

## 0. Ön Hazırlık

- [ ] Lokal API ayakta: `http://localhost:5041`
- [ ] Lokal DB ayakta: `Host=localhost;Port=5432;Database=CallCenterDB;Username=postgres`
- [ ] Iyzico sandbox env değişkenleri veya DB'de aktif config
- [ ] `curl`, `psql`, `dotnet`, `jq` kurulu
- [ ] Test JWT token'ı eldedeyse `.env` veya export edilmiş olarak hazır:
  ```bash
  export API=http://localhost:5041
  export JWT_OWNER=eyJhbGciOi...
  export JWT_ADMIN=eyJhbGciOi...
  export JWT_BRANCHMGR=eyJhbGciOi...
  ```

---

## 1. Build + Otomatik Test Suite

### 1.1. Solution Build
```bash
dotnet build CallCenter.slnx
```
Beklenen: 0 error, 0 warning (varsa preview SDK warning'i hariç).

### 1.2. Project-by-project Build
```bash
dotnet build src/CallCenter.Api/CallCenter.Api.csproj
dotnet build src/CallCenter.Salon/CallCenter.Salon.csproj
dotnet build src/CallCenter.Crm/CallCenter.Crm.csproj
dotnet build src/CallCenter.Management/CallCenter.Management.csproj
dotnet build src/CallCenter.Data/CallCenter.Data.csproj
dotnet build src/CallCenter.Shared/CallCenter.Shared.csproj
```

### 1.3. Unit / Integration Test Suite
```bash
dotnet test tests/CallCenter.Tests/CallCenter.Tests.csproj --logger "console;verbosity=normal"
```
Beklenen: Failed: 0, Passed: 339+ (mevcut baseline). Yeni test eklendiyse pass sayısı artar.

### 1.4. Coverage (opsiyonel)
```bash
dotnet test --collect:"XPlat Code Coverage"
# coverage report path: tests/CallCenter.Tests/TestResults/.../coverage.cobertura.xml
```
Hedef coverage: kritik factory'ler için %60+.

### 1.5. Kategori Bazlı Filtre
```bash
dotnet test --filter "FullyQualifiedName~SlnFinance"
dotnet test --filter "FullyQualifiedName~Loyalty"
dotnet test --filter "FullyQualifiedName~Payment"
```

---

## 2. Migration ve DB Şema

### 2.1. Migration Listesi
```bash
dotnet ef migrations list --project src/CallCenter.Data --startup-project src/CallCenter.Api
```
Tüm migration'lar `Applied` olmalı.

### 2.2. Snapshot Tutarlılığı
- [ ] `AppDbContextModelSnapshot.cs` son entity değişiklikleriyle senkron
- [ ] `dotnet ef migrations has-pending-model-changes` PASS

### 2.3. Sıfırdan Migrate Testi (temiz DB)
```sql
-- Test DB oluştur, sonra:
```
```bash
AUTO_MIGRATE=true dotnet run --project src/CallCenter.Api
# veya VS'de
```
Beklenen: hatasız tüm tabloları oluşturur, seed data dolar (default modules, type definitions).

### 2.4. Production Migration Smoke (dry-run)
```bash
dotnet ef migrations script --idempotent --project src/CallCenter.Data --startup-project src/CallCenter.Api -o /tmp/migration.sql
# Manuel review: DROP / ALTER COLUMN NOT NULL gibi tehlikeli komutlar var mı
```

---

## 3. Auth ve JWT Lifecycle

### 3.1. Login → Token
```bash
curl -s -X POST $API/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"owner@test.local","password":"Test1234!"}' | jq
```
Beklenen: 200, `{ token, refreshToken, expiresAt, user{...} }`.

### 3.2. Yanlış Şifre + Brute Force
```bash
for i in 1 2 3 4 5 6 7 8 9 10 11; do
  curl -s -o /dev/null -w "%{http_code}\n" -X POST $API/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"owner@test.local","password":"wrong"}'
done
```
Beklenen: ilk N denemede 401, N+1'de **423 Locked** veya 429.

### 3.3. JWT Expire
```bash
# Eski (5 dakika+ önce alınmış) token ile çağrı:
curl -i $API/api/customers -H "Authorization: Bearer $OLD_JWT"
```
Beklenen: 401, body `{"error":"token expired"}` veya WWW-Authenticate header.

### 3.4. Refresh Token
```bash
curl -s -X POST $API/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"...","accessToken":"..."}'
```

### 3.5. Logout/Revoke
```bash
curl -X POST $API/api/auth/logout -H "Authorization: Bearer $JWT"
# Sonra aynı token ile çağrı 401 dönmeli
```

### 3.6. Cross-Tenant İzolasyon
A müşterinin JWT'siyle B müşterinin verisine erişim:
```bash
curl -s $API/api/customers/{B_CUSTOMER_ID} -H "Authorization: Bearer $JWT_A"
```
Beklenen: 403/404. Asla başka tenant verisi sızmaz.

### 3.7. Role/Claim Manipulation
JWT'yi decode et, `CustomerRoleId` veya `CustomerModules` claim'ini değiştir, yeniden imzalanmadan kullan:
```bash
# Beklenen: 401 (signature invalid)
```

---

## 4. API Contract Test (Endpoint × Method × Auth)

### 4.1. Genel Pattern
Her endpoint için 3 vaka:
| Vaka | İstek | Beklenen |
|---|---|---|
| Yetkili kullanıcı | `Authorization: Bearer $JWT` | 200/201 |
| Yetkisiz kullanıcı | Auth yok | 401 |
| Yetkisi olmayan rol | Farklı rol JWT | 403 |
| Modülü olmayan | Modül atanmamış | 403 + `{moduleId:X}` |

### 4.2. Kritik Endpoint Smoke
```bash
# API meta
curl -s $API/healthz | jq

# Auth gerektiren
for path in api/customers api/users api/sln-clients api/sln-appointments api/sln-invoices; do
  echo "=== $path ==="
  curl -s -o /dev/null -w "%{http_code}\n" $API/$path -H "Authorization: Bearer $JWT_OWNER"
done
```

### 4.3. Module Gate
```bash
# Sadakat Programı modülü olmayan kullanıcı:
curl -i $API/api/crm/salon/loyalty-programs/programs -H "Authorization: Bearer $JWT_NOLOYALTY"
# Beklenen: 403 + module required
```

### 4.4. Branch Scope (BranchId claim baskınlığı)
```bash
# Şube müdürü (BranchId=2 claim'li) farklı branch query'si:
curl -s "$API/api/sln-clients?branchId=99" -H "Authorization: Bearer $JWT_BRANCHMGR2" | jq '. | length'
# Beklenen: 0 veya sadece branch 2 müşterileri (claim parametreyi override eder)
```

### 4.5. Pagination + Sıralama + Filter
```bash
curl -s "$API/api/customers?page=1&pageSize=20&sortBy=name&filter=test" -H "Authorization: Bearer $JWT" | jq '.totalCount, .items | length'
```

### 4.6. Validation Hataları
```bash
# Boş zorunlu alan:
curl -s -X POST $API/api/sln-clients \
  -H "Authorization: Bearer $JWT" -H "Content-Type: application/json" \
  -d '{}' | jq
# Beklenen: 400, validation errors object
```

### 4.7. Idempotency
```bash
# Aynı POST 2 kez:
PAYLOAD='{"name":"Test","phone":"5551112233","idempotencyKey":"abc-123"}'
curl -X POST $API/api/sln-clients -H "Authorization: Bearer $JWT" -H "Content-Type: application/json" -d "$PAYLOAD"
curl -X POST $API/api/sln-clients -H "Authorization: Bearer $JWT" -H "Content-Type: application/json" -d "$PAYLOAD"
# Beklenen: ikisi de aynı entity Id döner (varsa idempotency-key desteği)
```

### 4.8. Optimistic Concurrency
```bash
# A user GET → version
# B user GET → version
# A user PUT (version=X) → 200
# B user PUT (version=X) → 409 Conflict
```

---

## 5. DB State Assertion

Her UI/API testinden sonra **gerçek tablonun ne yazdığını** SQL ile doğrula.

### 5.1. Invoice + Cash Transaction
```sql
SELECT i."Id", i."InvoiceNo", i."NetAmount", i."DiscountAmount",
       (SELECT COUNT(*) FROM "SlnCashTransactions" ct WHERE ct."RelatedInvoiceId"=i."Id") AS cash_lines
FROM "SlnInvoices" i
WHERE i."CustomerId"=$CID ORDER BY i."Id" DESC LIMIT 5;
```
Her invoice için kasa hareketi sayısı en az 1 olmalı (NetAmount>0 ise).

### 5.2. Loyalty Earn/Spend Tutarlılığı
```sql
SELECT scl."Id" AS client_loyalty_id,
       scl."TotalEarned", scl."TotalSpent", scl."CurrentBalance",
       scl."TotalEarned" - scl."TotalSpent" AS computed_balance
FROM "SlnClientLoyalties" scl
WHERE scl."CustomerId"=$CID;
-- computed_balance == CurrentBalance olmalı (her satır)
```

### 5.3. Loyalty Transaction Toplam Doğrulaması
```sql
SELECT scl."Id",
       (SELECT COALESCE(SUM(t."Points"),0) FROM "SlnLoyaltyTransactions" t WHERE t."ClientLoyaltyId"=scl."Id" AND t."TransactionTypeId"=1) AS earn_sum,
       scl."TotalEarned",
       (SELECT COALESCE(SUM(t."Points"),0) FROM "SlnLoyaltyTransactions" t WHERE t."ClientLoyaltyId"=scl."Id" AND t."TransactionTypeId"=2) AS spend_sum,
       scl."TotalSpent"
FROM "SlnClientLoyalties" scl WHERE scl."CustomerId"=$CID;
-- earn_sum == TotalEarned, spend_sum == TotalSpent
```

### 5.4. Loyalty Package Bakiye
```sql
SELECT p."Id", p."ClientId", p."TotalSessions", p."UsedSessions", p."RemainingSessions",
       p."TotalSessions" - p."UsedSessions" AS computed_remaining
FROM "SlnLoyaltyPackagePurchases" p WHERE p."CustomerId"=$CID;
```

### 5.5. Multi-Session Plan vs Used
```sql
SELECT plan."Id", plan."TotalSessions",
       (SELECT COUNT(*) FROM "SlnServiceSessionRecords" r WHERE r."PlanId"=plan."Id") AS used,
       plan."TotalSessions" - (SELECT COUNT(*) FROM "SlnServiceSessionRecords" r WHERE r."PlanId"=plan."Id") AS remaining
FROM "SlnServiceSessionPlans" plan WHERE plan."CustomerId"=$CID;
```

### 5.6. Loyalty Program Reward Lifecycle
```sql
SELECT lp."Id", lp."ProgramId", lp."ClientLoyaltyProgressId",
       lp."ExpiresAt", lp."UsedAt",
       CASE
         WHEN lp."UsedAt" IS NOT NULL THEN 'used'
         WHEN lp."ExpiresAt" < NOW() THEN 'expired'
         ELSE 'available'
       END AS status
FROM "SlnLoyaltyProgramRewards" lp;
-- AvailableRewards CRM tarafında bu satırlardan 'available' olanların sayısına eşit olmalı
```

### 5.7. PaymentTransaction Statü Akışı
```sql
SELECT pt."Id", pt."Uid", pt."ProviderTransactionId",
       pt."StatusId", ps."Description",
       pt."ErrorMessage", pt."CreatedAt", pt."CompletedAt"
FROM "PaymentTransactions" pt
LEFT JOIN "TypeDefinitions" ps ON ps."Id" = pt."StatusId"
WHERE pt."CustomerId"=$CID ORDER BY pt."Id" DESC LIMIT 10;
```

### 5.8. Encrypted Credentials Decrypt Edilebiliyor mu
```powershell
# PaymentConfig EncryptedCredentials için PowerShell decrypt:
$key = [System.Security.Cryptography.SHA256]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes("CallCenter_AES_Encryption_Key_2026_Prod!!"))
# ... AES-256-CBC decrypt (örnek script docs/PAYSPLIT-sandbox-test.md veya ClaudeManager #139)
# Çıktı: { ApiKey, SecretKey, BaseUrl } — boş ApiKey/SecretKey OLMAMALI
```

### 5.9. Audit Trail
```sql
SELECT al."Id", al."Action", al."EntityName", al."EntityId",
       al."UserId", al."CustomerId", al."CreatedAt"
FROM "AuditLogs" al
WHERE al."CustomerId"=$CID ORDER BY al."CreatedAt" DESC LIMIT 50;
```
Sensitive değişikliklerde (rol değişimi, payment config edit, KVKK delete) kayıt olmalı.

### 5.10. Soft Delete Tutarlılığı
```sql
-- Eğer projemizde soft delete varsa (IsDeleted gibi):
SELECT * FROM "Customers" WHERE "IsDeleted"=true AND "DeletedAt" IS NULL;
-- Sıfır olmalı (her soft delete'in DeletedAt damgası olmalı)
```

---

## 6. Background Jobs / Cron / Worker

### 6.1. Subscription Tahakkuk
Her ayın 1'inde / belirli tarihte aboneliği aktif olan her customer için `BillingPeriod` + `PaymentTransaction (pending)` yaratılmalı.

- [ ] Lokal'de tarih ileri alarak job tetikle (varsa admin endpoint veya manuel SQL)
- [ ] Yeni `BillingPeriods` kaydı oluştu
- [ ] Otomatik gönderilen bildirim e-postası loglandı

### 6.2. No-Show Penalty
Randevu tarihi geçti, status `geldi` değil, müşteri `IsBlacklisted=false`:
- [ ] Job çalıştığında NoShow status'a düşer
- [ ] NoShowCount += 1
- [ ] Politika eşik aşıldıysa blacklist veya deposit forfeit

### 6.3. Kasa Açık Kapama Hatırlatıcı
- [ ] Gün sonunda kapatılmayan kasa için bildirim
- [ ] Birden çok gün açık kalan kasa için escalation

### 6.4. Cleanup Jobs
- [ ] Eski recording dosyaları S3'ten sil (retention policy)
- [ ] Expired loyalty reward'ları "expired" işaretle
- [ ] Eski auth attempt log'ları temizle

### 6.5. Gerçek Çalıştığını Doğrulama
Job ne zaman çalışıyor? Schedule'ı kim tutuyor (Hangfire, IHostedService, Cron, GCP Cloud Scheduler)?
- [ ] Lokal test için job'ı manuel tetikleyen test endpoint var mı? (`/api/dev/trigger-billing-cycle`)
- [ ] Job log/metrics nereye düşüyor?

---

## 7. SignalR / Realtime

### 7.1. Hub Connect
```javascript
// Node.js veya browser:
const { HubConnectionBuilder, LogLevel } = require('@microsoft/signalr');

const conn = new HubConnectionBuilder()
  .withUrl(`${API}/hubs/callcenter`, { accessTokenFactory: () => JWT })
  .configureLogging(LogLevel.Information)
  .build();

await conn.start();
console.log('connected', conn.connectionId);
```

### 7.2. Inbound Call Broadcast
- [ ] Test PBX'ten çağrı simüle et (örneğin admin endpoint `/api/dev/simulate-call`)
- [ ] Connected client `onIncomingCall` event aldı
- [ ] Çağrı tüm yetkili agent'lara broadcast oldu, yetkisizler almadı

### 7.3. Agent Status Sync
```javascript
await conn.invoke('SetStatus', 'Available');
// Diğer client'ta:
conn.on('agentStatusChanged', (agentId, status) => { ... });
```

### 7.4. Queue Update
- [ ] Yeni çağrı kuyruğa girdi → tüm supervisor client'a broadcast
- [ ] Queue length update <100ms

### 7.5. Notification
- [ ] Server-side notification trigger → SignalR ile push
- [ ] Toast yerine sadece event olarak gelir, UI değil

### 7.6. Disconnect / Reconnect
- [ ] Client disconnect → server side `OnDisconnectedAsync` çağrılır
- [ ] Auto-reconnect denemesi: 0s, 2s, 10s, 30s
- [ ] Reconnect sonrası state sync (recent calls, queue state)

### 7.7. Connection Limit
- [ ] 100+ eşzamanlı bağlantı kabul ediliyor
- [ ] Memory leak yok (1 saat aktif sonra heap stable)

---

## 8. Webhook (Async Inbound Event)

### 8.1. Iyzico Webhook Imza Doğrulama
```bash
PAYLOAD='{"eventType":"payment.completed","conversationId":"...","paymentId":"..."}'
SIGNATURE=$(echo -n "$PAYLOAD$SECRET" | openssl dgst -sha256 -binary | base64)

curl -X POST $API/api/payments/iyzico-webhook \
  -H "Content-Type: application/json" \
  -H "X-IYZ-SIGNATURE-V3: $SIGNATURE" \
  -d "$PAYLOAD"
```
- [ ] Geçerli signature → 200, transaction güncellendi
- [ ] Geçersiz signature → 401
- [ ] Eksik signature → 401
- [ ] Bilinmeyen eventType → 200 (idempotent, audit log'a yazar)

### 8.2. Replay Attack Koruma
Aynı webhook event ID 2 kez gönderildi → 200 ama tek effect.

### 8.3. Webhook Timeout Davranışı
Backend slow processing → Iyzico 30s timeout → retry.
- [ ] Idempotency anahtarı (paymentId) ile aynı işlem 2 kez işlenmez
- [ ] Webhook log tablosunda her deneme kayıtlı

### 8.4. Webhook URL Public Erişilebilir
Production: `https://sln.corplynk.com/api/payments/iyzico-webhook` (Cloudflare/proxy üzerinden)
- [ ] DNS resolve ediyor
- [ ] SSL geçerli
- [ ] HEAD request 405 değil 200/Method Not Allowed dönmeli

---

## 9. Email + SMS + Push Bildirim

### 9.1. Resend Email Smoke
```bash
curl -X POST $API/api/dev/send-test-email \
  -H "Authorization: Bearer $JWT_ADMIN" \
  -d '{"to":"test@example.com","template":"welcome"}'
```
- [ ] 200 + provider message ID
- [ ] Email gerçekten geldi (test inbox kontrol)
- [ ] Resend API rate limit aşımı 429 ile graceful

### 9.2. SMS Sandbox
- [ ] SMS provider sandbox'ında mesaj gönderildi
- [ ] Telefon normalize (+90 prefix) doğru çalışıyor

### 9.3. Push Notification (mobil)
- [ ] FCM/APNS sandbox token kayıtlı
- [ ] Notification gönderildi, device received (test cihazıyla)

### 9.4. Email Template Render
```bash
curl -X POST $API/api/email-templates/render \
  -H "Authorization: Bearer $JWT_ADMIN" \
  -d '{"templateId":1,"data":{"name":"Test"}}'
# Beklenen: rendered HTML, placeholders yerine değer yerleşmiş
```

### 9.5. Email Integration OAuth (Gmail/Outlook)
- [ ] OAuth code → token exchange
- [ ] Refresh token DB'de encrypted saklı
- [ ] Token expire olunca refresh otomatik

---

## 10. Cloud Storage (Recordings, Photos)

### 10.1. Provider Test
```bash
curl -X POST $API/api/storage-config/test/{id} -H "Authorization: Bearer $JWT_ADMIN"
# Beklenen: { success: true } veya hata mesajı
```

### 10.2. Upload + Download Roundtrip
```bash
# Upload test
curl -X POST $API/api/dev/storage-upload-test -F "file=@test.txt" -H "Authorization: Bearer $JWT_ADMIN"
# Returns: { url, objectKey }

# Download
curl -i $URL
# 200 + içerik
```

### 10.3. Signed URL Expire
- [ ] Generate signed URL, 1 saat sonra erişim 403 dönmeli
- [ ] Signed URL başkasına forward edilince yine çalışmalı (URL geçerli)

### 10.4. Provider Bazlı
- [ ] Google Drive OAuth token refresh
- [ ] OneDrive (Azure) OAuth token refresh
- [ ] Yandex Cloud OAuth
- [ ] S3 (varsa) IAM credentials

---

## 11. Iyzico Sandbox Integration (End-to-End API)

### 11.1. CheckoutForm Initialize (Direct)
```bash
# Backend üzerinden değil, doğrudan Iyzico'ya:
curl -X POST https://sandbox-api.iyzipay.com/payment/iyzipos/checkoutform/initialize/auth/ecom \
  -H "Authorization: IYZWSv2 $(...)" \
  -H "x-iyzi-rnd: $(...)" \
  -H "Content-Type: application/json" \
  -d @iyzico-request.json
```
Beklenen: `status: success`, `token`, `checkoutFormContent`, `paymentPageUrl`.

### 11.2. Callback Simulation
```bash
# Iyzico'nun göndereceği POST'u simüle et:
curl -X POST $API/api/payments/iyzico-callback \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=$TOKEN"
# Beklenen: HTML, parent window'a postMessage gönderir
```

### 11.3. PackageResult Polling
```bash
curl -X POST $API/api/payments/package-result \
  -H "Authorization: Bearer $JWT" \
  -d '{"token":"..."}'
# Status'a göre: success / pending / failed / cancelled
```

### 11.4. Sub-Merchant Onboarding (PS.4)
```bash
curl -X POST $API/api/payments/submerchant/onboard \
  -H "Authorization: Bearer $JWT" \
  -d @submerchant-payload.json
```
- [ ] iyzico onboard endpoint çağrılır
- [ ] Customer.SubMerchantKey DB'ye yazılır

### 11.5. Marketplace Split (PS.6/PS.7)
- [ ] basketItem'a subMerchantKey ve subMerchantPrice ekleniyor mu (request body inspect)
- [ ] Iyzico response'unda settlement bilgisi geliyor mu

### 11.6. Settlement Report
- [ ] `/api/payments/settlements?from=&to=` rapor döner
- [ ] %1 stopaj hesabı doğru (PS.10)

---

## 12. Translation Cache (i18n)

### 12.1. Cache Reload
```bash
curl -X POST $API/api/translations/reload-cache -H "Authorization: Bearer $JWT_ADMIN"
# Beklenen: 200, cache version bumped
```

### 12.2. Salon Server-Side Cache Stale Mi
```bash
# Önce Management'tan key değiştir
# Salon'a 30 saniye sonra istek at:
curl -i http://localhost:5239/Home -H "Cookie: CorpLynk.Salon.Auth=..."
# Response HTML'inde yeni çeviri görünmeli (SALONI18N.9 fix)
```

### 12.3. Tüm Diller İçin Key Mevcut
```sql
SELECT k."Id", k."KeyName",
  CASE WHEN v_tr."Value" IS NULL THEN 'MISSING' ELSE 'OK' END AS tr,
  CASE WHEN v_en."Value" IS NULL THEN 'MISSING' ELSE 'OK' END AS en
FROM "TranslationKeys" k
LEFT JOIN "TranslationValues" v_tr ON v_tr."KeyId"=k."Id" AND v_tr."Lang"='tr'
LEFT JOIN "TranslationValues" v_en ON v_en."KeyId"=k."Id" AND v_en."Lang"='en';
-- Hiç MISSING olmamalı (özellikle yeni eklenen key'lerde)
```

---

## 13. Security

### 13.1. Response Headers
```bash
curl -I https://sln.corplynk.com/ | grep -iE "content-security|x-frame|x-content|referrer|permissions"
```
Beklenen header'ların hepsi mevcut.

### 13.2. CSP Compliance
- [ ] Iyzico bundle yüklenebiliyor (script-src `https://*.iyzipay.com` var)
- [ ] Inline script execute oluyor (`'unsafe-inline'` var)
- [ ] Browser console'da CSP violation YOK

### 13.3. SSRF Koruması
```bash
# Proxy endpoint'ler external URL'ye atış engellenmeli (4fec31f):
curl -i "$API/proxy/some?url=http://evil.com" -H "Authorization: Bearer $JWT"
# Beklenen: 400 veya 403
```

### 13.4. SQL Injection
```bash
curl -s "$API/api/customers?search=' OR 1=1--" -H "Authorization: Bearer $JWT"
# Beklenen: Normal arama davranışı (boş veya filtreli sonuç), 500 OLMAMALI
```

### 13.5. XSS Payload
```bash
curl -X POST $API/api/sln-clients \
  -H "Authorization: Bearer $JWT" -H "Content-Type: application/json" \
  -d '{"name":"<script>alert(1)</script>","phone":"5551112233"}'
# Sonra GET'te: response JSON içinde escape edilmiş veya raw (frontend escape eder)
```

### 13.6. CSRF AntiForgery
MVC form POST'unda `__RequestVerificationToken` yoksa 400. AJAX POST'ta header üzerinden token.

### 13.7. Rate Limit
```bash
for i in $(seq 1 200); do
  curl -s -o /dev/null -w "%{http_code}\n" $API/api/some-endpoint -H "Authorization: Bearer $JWT" &
done
wait
# 100+ den sonra 429 görmeli (varsa rate limit)
```

### 13.8. API Key Middleware
```bash
curl -i $API/api/integration/v1/contacts
# 401 (X-Api-Key yok)

curl -i $API/api/integration/v1/contacts -H "X-Api-Key: invalid"
# 401

curl -i $API/api/integration/v1/contacts -H "X-Api-Key: $VALID_KEY"
# 200
```

### 13.9. Encryption at Rest Doğrulama
- [ ] `PlatformPaymentConfig.EncryptedCredentials` base64 olarak değişken uzunlukta
- [ ] Direct SQL ile okunduğunda plaintext görülmez
- [ ] Aynı clear text 2 kez encrypt edilince **farklı** cipher (random IV)

### 13.10. JWT Secret Leak Test
- [ ] JWT secret env variable'da, repo'da değil
- [ ] Production'da secret > 32 byte
- [ ] Sembolik test: aynı secret kullanan başka sistem (örn. eski env) varsa yenile

---

## 14. Concurrency / Race Condition

### 14.1. Double-Submit Payment
```bash
PAYLOAD='{"paymentContext":"all","billingPeriodIds":[123]}'
curl -X POST $API/api/payments/checkout-session -H "Authorization: Bearer $JWT" -d "$PAYLOAD" &
curl -X POST $API/api/payments/checkout-session -H "Authorization: Bearer $JWT" -d "$PAYLOAD" &
wait
# Beklenen: bir tanesi success, diğeri "İşlem zaten başlamış" (409 veya 400)
```

### 14.2. Concurrent Booking
İki kullanıcı aynı slot'a randevu alıyor:
```bash
curl -X POST $API/api/sln-appointments -H "Authorization: Bearer $JWT" -d "$SLOT_PAYLOAD" &
curl -X POST $API/api/sln-appointments -H "Authorization: Bearer $JWT2" -d "$SLOT_PAYLOAD" &
wait
# Beklenen: biri 201, diğeri 409 conflict
```

### 14.3. Inventory Decrement
Stok 1 olan ürün 5 farklı satışta aynı anda kullanılıyor:
- [ ] Sadece 1 satış başarılı, diğerleri "Yetersiz stok" hata

### 14.4. Loyalty Point Redeem Race
Müşteri 100 puan, 2 farklı kasiyer 80'er puan kullan dedi:
- [ ] Toplam 200 değil 100 puan kullanılır, biri reddedilir

---

## 15. Performance / Load

### 15.1. Endpoint Latency Hedefleri (P95)
| Endpoint | Hedef |
|---|---|
| GET /api/customers (sayfalı) | < 200ms |
| GET /api/sln-appointments (gün) | < 300ms |
| GET /api/sln-clients/{id} | < 150ms |
| POST /api/sln-invoices | < 500ms |
| GET /api/reports/daily-revenue | < 1s |

### 15.2. Load Test (k6)
```javascript
import http from 'k6/http';
export const options = { vus: 50, duration: '60s' };
export default function () {
  http.get(`${API}/api/customers?page=1&pageSize=20`, {
    headers: { Authorization: `Bearer ${__ENV.JWT}` }
  });
}
```
Hedef: 50 concurrent VU'da p95 < 500ms, error rate < 1%.

### 15.3. DB Query Plan Spot Check
```sql
EXPLAIN ANALYZE
SELECT * FROM "SlnClients" sc
WHERE sc."CustomerId"=$1 AND sc."FullName" ILIKE '%test%'
ORDER BY sc."CreatedAt" DESC LIMIT 20;
-- Beklenen: Index Scan, < 50ms (uygun index'le)
```

### 15.4. Memory Profil (uzun çalışma)
- [ ] API süreci 24 saat çalıştır, memory profile: GC stabil, leak yok
- [ ] SignalR aktif connection 100 saatte memory 200MB'tan az artış

### 15.5. CPU Profil (sıcak path)
```bash
dotnet-trace collect --process-id $(pgrep -f CallCenter.Api) --duration 00:00:30
# Hangi metod CPU yiyor?
```

---

## 16. Observability

### 16.1. Log Seviyeleri
- [ ] Production: `Information` ve üzeri
- [ ] Hassas veri (password, token) log'da görünmüyor
- [ ] Structured logging (JSON) GCP Logging'e gidiyor

### 16.2. Metrics
- [ ] `/metrics` endpoint Prometheus formatı veya GCP Monitoring
- [ ] Request count, latency, error rate
- [ ] Database connection pool stats

### 16.3. Health Check
```bash
curl -s $API/healthz | jq
# Beklenen: { status: "Healthy", checks: [{ name: "db", status: "Healthy" }, ...] }
```

### 16.4. Dependency Health
- [ ] DB connection check
- [ ] External API ping (Iyzico, Resend)
- [ ] Storage provider ping

### 16.5. Distributed Tracing
- [ ] Request → API → DB chain için correlation ID
- [ ] Trace ID response header'da

---

## 17. CI / Deployment Pipeline

### 17.1. GitHub Actions / CI
- [ ] PR açıldığında build + test otomatik koşar
- [ ] Test fail → merge bloke
- [ ] Lint / format kontrolü (varsa)

### 17.2. Build Artifact
```bash
dotnet publish src/CallCenter.Api -c Release -o /tmp/publish
# Boyut kontrolü (< 500MB), gereksiz dosya yok
```

### 17.3. Docker Image
```bash
docker build -t callcenter-api .
docker run -p 5041:8080 --env-file .env callcenter-api
curl http://localhost:5041/healthz
```
- [ ] Image boyutu kabul edilebilir (< 300MB)
- [ ] Non-root user
- [ ] Secret ENV'de değil mount edilmiş

### 17.4. Cloud Run Deploy
```bash
gcloud run deploy callcenter-api --image gcr.io/.../callcenter-api \
  --update-env-vars KEY=VALUE  # ASLA --set-env-vars
```
- [ ] Sıfır downtime (revision traffic split)
- [ ] Rollback komutu hazır

### 17.5. Post-Deploy Smoke
```bash
curl -i https://cc-api.corplynk.com/healthz
curl -i https://sln.corplynk.com/
curl -i https://mng.corplynk.com/
```

---

## 18. Disaster Recovery / Backup

### 18.1. DB Backup
- [ ] Otomatik snapshot her gün
- [ ] Restore prosedürü test edildi (lokal'e indirip yükle)
- [ ] Point-in-time recovery testi

### 18.2. Encryption Key Rotation
- [ ] Key değişimi nasıl yapılır (mevcut data re-encrypt)
- [ ] Eski key ile şifrelenmiş veri yeni key ile decrypt edilebilmeli (key versioning varsa)

### 18.3. Secret Rotation
- [ ] DB password değişimi
- [ ] JWT secret değişimi (mevcut token'lar invalid olur)
- [ ] Iyzico API key değişimi

---

## 19. Test Sonuç Raporu

Her bölüm sonunda:
```
[Bölüm X.Y Başlık]
PASS / FAIL / SKIP
Komut: <çalıştırılan curl/sql>
Çıktı: <kısa>
Notlar:
DB satır sayısı/state değişimi:
```

FAIL → ClaudeManager Task aç, fix sonrası tekrar koş.

---

## 20. Çalışma Sırası

1. **Bölüm 1** — Build + dotnet test (her commit/PR) — 5 dk
2. **Bölüm 2** — Migration sanity (release öncesi) — 10 dk
3. **Bölüm 3** — Auth lifecycle — 20 dk
4. **Bölüm 4** — API contract smoke — 30 dk
5. **Bölüm 5** — DB state assertion (her E2E akış sonrası) — değişken
6. **Bölüm 6** — Background jobs — 30 dk
7. **Bölüm 7** — SignalR — 20 dk
8. **Bölüm 8** — Webhook — 15 dk
9. **Bölüm 9-10** — Email/SMS/Storage — 20 dk
10. **Bölüm 11** — Iyzico sandbox E2E — 30 dk
11. **Bölüm 12** — Translation cache — 10 dk
12. **Bölüm 13** — Security — 1 saat
13. **Bölüm 14** — Concurrency — 30 dk
14. **Bölüm 15** — Performance/Load — 1 saat
15. **Bölüm 16** — Observability — 20 dk
16. **Bölüm 17** — CI/Deploy pipeline — 30 dk
17. **Bölüm 18** — DR/Backup (quarterly) — 30 dk

Tam kapsam: **~7 saat** sistem testi (UI testleri ayrı 16 saat — `TEST_MAP.md`).

---

**Doküman versiyonu:** 1.0 — 2026-06-02
**İlgili dokümanlar:**
- `TEST_MAP.md` — UI / click test haritası
- ClaudeManager pattern #595, #597 — sadakat scope
- ClaudeManager note #139, #152 — Iyzico credentials

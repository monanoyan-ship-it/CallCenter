# PAYSPLIT — Sandbox/Test Ortami (PS.11)

iyzico Pazaryeri sub-merchant onboarding ve odeme akisi icin sandbox test rehberi.

## 1. Sandbox config (DB)

iyzico API anahtarlari `PlatformPaymentConfigs` tablosunda saklanir; Management UI uzerinden ayarlanir (kod tarafinda `appsettings` icinde TUTULMAZ).

**Management → Odeme Ayarlari** sayfasindan yeni config olustur:

| Alan | Sandbox Deger |
|------|---------------|
| ProviderType | Iyzico |
| ApiKey | `sandbox-XXXXXXXX...` (iyzico merchant.iyzipay.com sandbox panel) |
| SecretKey | `sandbox-YYYYYYYY...` |
| IsSandbox | `true` (BaseUrl otomatik `https://sandbox-api.iyzipay.com`) |
| IsActive | `true` |

`PaymentConfigFactory.cs:222` — `IsSandbox=true` ise BaseUrl `sandbox-api.iyzipay.com`'a set edilir.

## 2. Sandbox merchant hesabi

iyzico merchant panel: <https://merchant.iyzipay.com>
- Tek seferlik kayit, "Sandbox" sekmesinde API anahtarlari hazir gelir.
- Sandbox-da KYC istemez; sub-merchant olusturmak icin mock TCKN/VKN/IBAN yeterlidir.

## 3. Mock test verileri

### Bireysel sub-merchant (Sahis)
```json
{
  "subMerchantType": "PERSONAL",
  "contactName": "Test",
  "contactSurname": "Salon",
  "identityNumber": "11111111110",
  "iban": "TR550006400000011234567890",
  "gsmNumber": "+905551112233",
  "address": "Test Mah. Test Cad. No:1 Test/ISTANBUL"
}
```

### Sirket sub-merchant
```json
{
  "subMerchantType": "PRIVATE_COMPANY",
  "name": "Test Salon Ltd. Sti.",
  "taxOffice": "Kadikoy",
  "taxNumber": "1234567890",
  "iban": "TR550006400000011234567890",
  "gsmNumber": "+905551112233",
  "address": "Test Mah. Test Cad. No:1 Test/ISTANBUL"
}
```

## 4. Test kart numaralari (3DS)

Iyzico sandbox'ta basari/hata simulasyonu icin standart test kartlari:

| Numara | Sonuc |
|--------|-------|
| `5528790000000008` | Basari (Master) |
| `4766620000000001` | Basari (Visa) |
| `5406670000000009` | Basari (Master) |
| `4543590000000006` | 3DS basari |
| `4111111111111111` | INSUFFICIENT_FUNDS hatasi |
| `4129111111111111` | DO_NOT_HONOUR hatasi |

CVV: `000`, son kullanim: gelecek bir tarih (12/30 vb.), CardHolderName istenildigi gibi.

## 5. Manuel curl test — sub-merchant onboarding (PS.4)

```bash
# Token al (salon admin, CustomerUser)
TOKEN=$(curl -s -X POST http://localhost:5041/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin@testsalon.com","password":"Test123!"}' \
  | jq -r '.token')

# Sub-merchant onboarding
curl -X POST http://localhost:5041/api/payments/sub-merchant \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "subMerchantType": "PERSONAL",
    "contactName": "Test",
    "contactSurname": "Salon",
    "identityNumber": "11111111110",
    "iban": "TR550006400000011234567890",
    "gsmNumber": "+905551112233",
    "address": "Test Mah. Test Cad. No:1 Test/ISTANBUL"
  }'
```

Beklenen yanit: `{ "success": true, "subMerchantKey": "iyz-..." }`. SlnSalonProfile.IyzicoSubMerchantKey + IyzicoOnboardingStatus=2 (active) DB'ye yazilir.

## 6. Manuel curl test — randevu sonrasi tahsilat (PS.15)

```bash
# Platform user token (musteri)
PT=$(curl -s -X POST http://localhost:5041/api/platform/login \
  -H "Content-Type: application/json" \
  -d '{"phone":"5551112233","password":"Test123!"}' \
  | jq -r '.token')

# Pay-checkout (appointmentId=42)
curl -X POST http://localhost:5041/api/platform/appointments/42/pay-checkout \
  -H "Authorization: Bearer $PT" \
  -H "Content-Type: application/json" \
  -d '{}'
```

Beklenen yanit: `{ "success": true, "htmlContent": "<form>...</form>", "token": "iyz-..." }`. HTML iframe icinde acilir, kullanici 3DS form doldurur, callback'e doner.

## 7. Hata senaryolari

- **`Salon online tahsilat icin hazir degil`** — SlnSalonProfile sub-merchant onboarded degil. PS.4 onboarding endpoint'i once cagrilmali.
- **`Bu randevu icin odenecek tutar kalmadi`** — TotalPrice <= mevcut PayAppointment tx Sum.
- **`Iptal veya gelinmedi durumundaki randevu...`** — StatusId 4 veya 5.
- **3DS simulasyon callback'i** — sandbox'ta gelmezse `https://sandbox-api.iyzipay.com/payment/iyzipos/checkoutform/auth/ecom` test edilebilir.

## 8. Otomatik test

Henuz xUnit test yok (PS.11 manuel test rehberi). Olusturulacaksa:
- `tests/CallCenter.Tests/PaymentSplitTests.cs` (TODO)
- `OnboardSubMerchantAsync` mock IyzicoGateway ile
- `InitPayAppointmentCheckoutAsync` happy path + sub-merchant eksik hatasi
- `GetMarketplaceSplitAsync` komisyon hesabi (5%, 10%, override)

Ilgili journal: #361 (PS.6 deposit decision), #378 (mobile MOBQA.12), #383 (mobile Phase 8-11).

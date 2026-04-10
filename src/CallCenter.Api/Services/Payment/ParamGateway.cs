using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;

namespace CallCenter.Api.Services.Payment;

/// <summary>
/// Param (eski Paraü/Türkpos) odeme gateway implementasyonu.
/// Param SOAP/REST API ile calısır.
/// Dokumasyon: https://dev.param.com.tr/
/// </summary>
public class ParamGateway : IPaymentGateway
{
    private readonly ParamCredentials _credentials;
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public int ProviderTypeId => PaymentProviders.Ids.Param;

    public ParamGateway(ParamCredentials credentials)
    {
        _credentials = credentials;
        _http = new HttpClient { BaseAddress = new Uri(credentials.BaseUrl) };
    }

    public async Task<PaymentInitResult> InitiatePaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            // Param TP_WMD_UCD endpoint (Non-3D direkt odeme)
            var totalAmount = request.Amount.ToString("F2").Replace(",", ".");
            var installment = request.Installment <= 0 ? 1 : request.Installment;

            var hashData = _credentials.ClientCode + _credentials.Guid + totalAmount +
                installment.ToString() + request.ConversationId;
            var hash = ComputeSha256(hashData);

            var payload = new
            {
                CLIENT_CODE = _credentials.ClientCode,
                CLIENT_USERNAME = _credentials.ClientUsername,
                CLIENT_PASSWORD = _credentials.ClientPassword,
                GUID = _credentials.Guid,
                KK_Sahibi = request.CardHolderName ?? "",
                KK_No = request.CardNumber ?? "",
                KK_SK_Ay = request.ExpireMonth ?? "",
                KK_SK_Yil = request.ExpireYear ?? "",
                KK_CVC = request.Cvc ?? "",
                KK_Sahibi_GSM = request.BuyerPhone ?? "",
                Hata_URL = request.CallbackUrl ?? "https://corplynk.com/payment/fail",
                Basarili_URL = request.CallbackUrl ?? "https://corplynk.com/payment/success",
                Siparis_ID = request.ConversationId,
                Siparis_Aciklama = request.Description ?? "Corplynk Odeme",
                Taksit = installment,
                Islem_Tutar = totalAmount,
                Toplam_Tutar = totalAmount,
                Islem_Hash = hash,
                Islem_Guvenlik_Tip = "NS", // Non-Secure
                IP_Adresi = request.BuyerIp ?? "127.0.0.1",
                Para_Birimi = request.Currency == "TRY" ? "TL" : request.Currency
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/turkpos/rest/Non3D/TP_WMD_UCD", content, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<ParamPaymentResponse>(json, JsonOpts);

            if (result?.Islem_Sonuc == "1" || result?.Sonuc == "1")
            {
                return PaymentInitResult.Ok(
                    result.Dekont_ID ?? request.ConversationId,
                    result.UCD_Ref_No);
            }

            return PaymentInitResult.Fail(result?.Sonuc_Str ?? result?.UCD_MD ?? "Param ödeme başarısız.");
        }
        catch (Exception ex)
        {
            return PaymentInitResult.Fail($"Param hatası: {ex.Message}");
        }
    }

    public async Task<PaymentVerifyResult> VerifyPaymentAsync(string providerTransactionId, CancellationToken ct = default)
    {
        return await Task.FromResult(PaymentVerifyResult.Ok(providerTransactionId));
    }

    public async Task<PaymentRefundResult> RefundAsync(string providerTransactionId, decimal amount, CancellationToken ct = default)
    {
        try
        {
            var totalAmount = amount.ToString("F2").Replace(",", ".");

            var payload = new
            {
                CLIENT_CODE = _credentials.ClientCode,
                CLIENT_USERNAME = _credentials.ClientUsername,
                CLIENT_PASSWORD = _credentials.ClientPassword,
                GUID = _credentials.Guid,
                Durum = 0, // Iade
                Tutar = totalAmount,
                Ref_URL = "",
                Data1 = providerTransactionId,
                Data2 = "",
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/turkpos/rest/TP_Islem_Iptal_Iade_Kismi", content, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<ParamRefundResponse>(json, JsonOpts);

            if (result?.Sonuc == "1")
                return PaymentRefundResult.Ok(providerTransactionId);

            return PaymentRefundResult.Fail(result?.Sonuc_Str ?? "İade başarısız.");
        }
        catch (Exception ex)
        {
            return PaymentRefundResult.Fail($"Param iade hatası: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            // BIN sorgulama ile test
            var payload = new
            {
                CLIENT_CODE = _credentials.ClientCode,
                CLIENT_USERNAME = _credentials.ClientUsername,
                CLIENT_PASSWORD = _credentials.ClientPassword,
                GUID = _credentials.Guid,
                BIN = "454360"
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/turkpos/rest/TP_Islem_Odeme/BIN_SanalPos", content, ct);
            return (response.IsSuccessStatusCode, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string ComputeSha256(string data)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(bytes);
    }

    private class ParamPaymentResponse
    {
        public string? Islem_Sonuc { get; set; }
        public string? Sonuc { get; set; }
        public string? Sonuc_Str { get; set; }
        public string? Dekont_ID { get; set; }
        public string? UCD_Ref_No { get; set; }
        public string? UCD_MD { get; set; }
    }

    private class ParamRefundResponse
    {
        public string? Sonuc { get; set; }
        public string? Sonuc_Str { get; set; }
    }
}

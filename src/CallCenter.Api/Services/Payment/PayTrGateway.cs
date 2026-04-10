using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;

namespace CallCenter.Api.Services.Payment;

/// <summary>
/// PayTR odeme gateway implementasyonu.
/// PayTR iFrame API kullanir.
/// Dokumasyon: https://dev.paytr.com/
/// </summary>
public class PayTrGateway : IPaymentGateway
{
    private readonly PayTrCredentials _credentials;
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public int ProviderTypeId => PaymentProviders.Ids.PayTR;

    public PayTrGateway(PayTrCredentials credentials)
    {
        _credentials = credentials;
        _http = new HttpClient { BaseAddress = new Uri(credentials.BaseUrl) };
    }

    public async Task<PaymentInitResult> InitiatePaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            var merchantOid = request.ConversationId;
            var paymentAmount = ((int)(request.Amount * 100)).ToString(); // Kurus cinsinden
            var userBasket = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"[[\"Hizmet\", \"{request.Amount:F2}\", 1]]"));

            var hashStr = string.Join("",
                _credentials.MerchantId,
                request.BuyerIp ?? "127.0.0.1",
                merchantOid,
                request.BuyerEmail ?? "customer@corplynk.com",
                paymentAmount,
                userBasket,
                "0", // no_installment
                "0", // max_installment
                request.Currency ?? "TL",
                "1"  // test_mode
            );

            var paytrToken = ComputeHmac(hashStr, _credentials.MerchantKey + _credentials.MerchantSalt);

            var formData = new Dictionary<string, string>
            {
                ["merchant_id"] = _credentials.MerchantId,
                ["user_ip"] = request.BuyerIp ?? "127.0.0.1",
                ["merchant_oid"] = merchantOid,
                ["email"] = request.BuyerEmail ?? "customer@corplynk.com",
                ["payment_amount"] = paymentAmount,
                ["paytr_token"] = paytrToken,
                ["user_basket"] = userBasket,
                ["debug_on"] = "1",
                ["no_installment"] = "0",
                ["max_installment"] = "0",
                ["user_name"] = request.BuyerName ?? "Customer",
                ["user_phone"] = request.BuyerPhone ?? "",
                ["merchant_ok_url"] = request.CallbackUrl ?? "https://corplynk.com/payment/success",
                ["merchant_fail_url"] = request.CallbackUrl ?? "https://corplynk.com/payment/fail",
                ["timeout_limit"] = "30",
                ["currency"] = request.Currency ?? "TL",
                ["test_mode"] = "1",
                ["lang"] = "tr"
            };

            // Kart bilgileri (non-3D icin)
            if (!string.IsNullOrEmpty(request.CardNumber))
            {
                formData["cc_owner"] = request.CardHolderName ?? "";
                formData["card_number"] = request.CardNumber;
                formData["expiry_month"] = request.ExpireMonth ?? "";
                formData["expiry_year"] = request.ExpireYear ?? "";
                formData["cvv"] = request.Cvc ?? "";
            }

            var response = await _http.PostAsync("/odeme/api/get-token",
                new FormUrlEncodedContent(formData), ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<PayTrTokenResponse>(json, JsonOpts);

            if (result?.Status == "success" && !string.IsNullOrEmpty(result.Token))
            {
                var iframeHtml = $"<script src=\"https://www.paytr.com/js/iframeResizer.min.js\"></script>" +
                    $"<iframe src=\"https://www.paytr.com/odeme/guvenli/{result.Token}\" id=\"paytriframe\" " +
                    $"frameborder=\"0\" scrolling=\"no\" style=\"width:100%;min-height:400px;\"></iframe>" +
                    $"<script>iFrameResize({{}},'#paytriframe');</script>";

                return PaymentInitResult.Redirect(iframeHtml, merchantOid);
            }

            return PaymentInitResult.Fail(result?.Reason ?? "PayTR token alınamadı.");
        }
        catch (Exception ex)
        {
            return PaymentInitResult.Fail($"PayTR hatası: {ex.Message}");
        }
    }

    public async Task<PaymentVerifyResult> VerifyPaymentAsync(string providerTransactionId, CancellationToken ct = default)
    {
        // PayTR callback ile bildirim yapar — bu metod callback'ten gelen veriyi dogrular
        // Simdilik basit implementasyon
        return await Task.FromResult(PaymentVerifyResult.Ok(providerTransactionId));
    }

    public async Task<PaymentRefundResult> RefundAsync(string providerTransactionId, decimal amount, CancellationToken ct = default)
    {
        try
        {
            var paymentAmount = ((int)(amount * 100)).ToString();
            var hashStr = _credentials.MerchantId + providerTransactionId + paymentAmount + _credentials.MerchantSalt;
            var paytrToken = ComputeHmac(hashStr, _credentials.MerchantKey);

            var formData = new Dictionary<string, string>
            {
                ["merchant_id"] = _credentials.MerchantId,
                ["merchant_oid"] = providerTransactionId,
                ["return_amount"] = paymentAmount,
                ["paytr_token"] = paytrToken
            };

            var response = await _http.PostAsync("/odeme/iade", new FormUrlEncodedContent(formData), ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<PayTrRefundResponse>(json, JsonOpts);

            if (result?.Status == "success")
                return PaymentRefundResult.Ok(providerTransactionId);

            return PaymentRefundResult.Fail(result?.ErrMsg ?? "İade başarısız.");
        }
        catch (Exception ex)
        {
            return PaymentRefundResult.Fail($"PayTR iade hatası: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("/", ct);
            return (response.IsSuccessStatusCode, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string ComputeHmac(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    private class PayTrTokenResponse { public string? Status { get; set; } public string? Token { get; set; } public string? Reason { get; set; } }
    private class PayTrRefundResponse { public string? Status { get; set; } public string? ErrMsg { get; set; } }
}

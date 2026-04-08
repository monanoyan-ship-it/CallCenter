using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;

namespace CallCenter.Api.Services.Payment;

/// <summary>
/// Iyzico odeme gateway implementasyonu.
/// Iyzico REST API ile direkt HTTP cagrilari yapar (NuGet SDK yerine).
/// Dokumasyon: https://dev.iyzipay.com/
/// </summary>
public class IyzicoGateway : IPaymentGateway
{
    private readonly IyzicoCredentials _credentials;
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public int ProviderTypeId => PaymentProviders.Ids.Iyzico;

    public IyzicoGateway(IyzicoCredentials credentials)
    {
        _credentials = credentials;
        _http = new HttpClient { BaseAddress = new Uri(credentials.BaseUrl) };
    }

    public async Task<PaymentInitResult> InitiatePaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var conversationId = request.ConversationId;
        var priceTxt = request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        var body = new
        {
            locale = "tr",
            conversationId,
            price = priceTxt,
            paidPrice = priceTxt,
            currency = request.Currency,
            installment = request.Installment <= 1 ? 1 : request.Installment,
            basketId = conversationId,
            paymentChannel = "WEB",
            paymentGroup = "PRODUCT",
            paymentCard = new
            {
                cardHolderName = request.CardHolderName,
                cardNumber = request.CardNumber,
                expireMonth = request.ExpireMonth,
                expireYear = request.ExpireYear,
                cvc = request.Cvc,
                registerCard = "0"
            },
            buyer = new
            {
                id = conversationId,
                name = GetFirstName(request.BuyerName),
                surname = GetLastName(request.BuyerName),
                email = request.BuyerEmail ?? "noreply@corplynk.com",
                identityNumber = "11111111111",
                registrationAddress = "Turkiye",
                ip = request.BuyerIp ?? "127.0.0.1",
                city = "Istanbul",
                country = "Turkey"
            },
            shippingAddress = new { contactName = request.BuyerName ?? "Musteri", city = "Istanbul", country = "Turkey", address = "Turkiye" },
            billingAddress = new { contactName = request.BuyerName ?? "Musteri", city = "Istanbul", country = "Turkey", address = "Turkiye" },
            basketItems = new[]
            {
                new
                {
                    id = conversationId,
                    name = request.Description ?? "Odeme",
                    category1 = "Hizmet",
                    itemType = "VIRTUAL",
                    price = priceTxt
                }
            }
        };

        var json = JsonSerializer.Serialize(body);
        var httpRequest = CreateRequest(HttpMethod.Post, "/payment/auth", json);

        try
        {
            var response = await _http.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<IyzicoPaymentResponse>(responseBody, JsonOpts);

            if (result?.Status == "success")
            {
                return PaymentInitResult.Ok(
                    result.PaymentId ?? conversationId,
                    result.PaymentId);
            }

            return PaymentInitResult.Fail(result?.ErrorMessage ?? "Iyzico odeme hatasi");
        }
        catch (Exception ex)
        {
            return PaymentInitResult.Fail($"Iyzico baglanti hatasi: {ex.Message}");
        }
    }

    public async Task<PaymentVerifyResult> VerifyPaymentAsync(string providerTransactionId, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new { locale = "tr", paymentId = providerTransactionId });
        var httpRequest = CreateRequest(HttpMethod.Post, "/payment/detail", body);

        try
        {
            var response = await _http.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<IyzicoPaymentResponse>(responseBody, JsonOpts);

            return result?.Status == "success"
                ? PaymentVerifyResult.Ok(result.PaymentId ?? providerTransactionId)
                : PaymentVerifyResult.Fail(result?.ErrorMessage ?? "Dogrulama basarisiz");
        }
        catch (Exception ex)
        {
            return PaymentVerifyResult.Fail($"Iyzico dogrulama hatasi: {ex.Message}");
        }
    }

    public async Task<PaymentRefundResult> RefundAsync(string providerTransactionId, decimal amount, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            locale = "tr",
            paymentTransactionId = providerTransactionId,
            price = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            currency = "TRY"
        });
        var httpRequest = CreateRequest(HttpMethod.Post, "/payment/refund", body);

        try
        {
            var response = await _http.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<IyzicoPaymentResponse>(responseBody, JsonOpts);

            return result?.Status == "success"
                ? PaymentRefundResult.Ok(result.PaymentId ?? providerTransactionId)
                : PaymentRefundResult.Fail(result?.ErrorMessage ?? "Iade basarisiz");
        }
        catch (Exception ex)
        {
            return PaymentRefundResult.Fail($"Iyzico iade hatasi: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> TestConnectionAsync(CancellationToken ct = default)
    {
        // Iyzico API'sinin calisirligi testi: bin number sorgulama ile
        var body = JsonSerializer.Serialize(new { locale = "tr", binNumber = "454671" });
        var httpRequest = CreateRequest(HttpMethod.Post, "/payment/bin/check", body);

        try
        {
            var response = await _http.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<IyzicoPaymentResponse>(responseBody, JsonOpts);

            return result?.Status == "success"
                ? (true, null)
                : (false, result?.ErrorMessage ?? "API credential'lari gecersiz");
        }
        catch (Exception ex)
        {
            return (false, $"Iyzico baglanti hatasi: {ex.Message}");
        }
    }

    // ─── Iyzico Auth Header Olusturma ───

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string jsonBody)
    {
        var randomHeaderValue = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{new Random().Next(100000, 999999)}";
        var pkiString = $"[{path}]{jsonBody}"; // Simplified PKI
        var hashStr = $"{_credentials.ApiKey}{randomHeaderValue}{_credentials.SecretKey}{pkiString}";
        var hash = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(hashStr)));
        var authorizationHeader = $"IYZWS {_credentials.ApiKey}:{hash}";

        var request = new HttpRequestMessage(method, path)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        request.Headers.TryAddWithoutValidation("x-iyzi-rnd", randomHeaderValue);

        return request;
    }

    private static string GetFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Musteri";
        var parts = fullName.Trim().Split(' ');
        return parts[0];
    }

    private static string GetLastName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return ".";
        var parts = fullName.Trim().Split(' ');
        return parts.Length > 1 ? string.Join(" ", parts[1..]) : ".";
    }

    // ─── Iyzico Response Model ───

    private class IyzicoPaymentResponse
    {
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
        public string? PaymentId { get; set; }
        public string? ConversationId { get; set; }
    }
}

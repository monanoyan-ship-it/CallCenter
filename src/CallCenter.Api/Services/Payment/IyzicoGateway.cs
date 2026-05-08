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

        // Pazaryeri split — basketItem'a subMerchantKey + subMerchantPrice (varsa).
        var basketItem = new Dictionary<string, object?>
        {
            ["id"] = conversationId,
            ["name"] = request.Description ?? "Odeme",
            ["category1"] = "Hizmet",
            ["itemType"] = "VIRTUAL",
            ["price"] = priceTxt
        };
        if (!string.IsNullOrWhiteSpace(request.SubMerchantKey) && request.SubMerchantPrice.HasValue)
        {
            basketItem["subMerchantKey"] = request.SubMerchantKey;
            basketItem["subMerchantPrice"] = request.SubMerchantPrice.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }

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
            basketItems = new[] { basketItem }
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

    /// <summary>
    /// iyzico Pazaryeri sub-merchant olustur (PS.3).
    /// Endpoint: POST /onboarding/submerchant
    /// </summary>
    public async Task<IyzicoSubMerchantOnboardResult> CreateSubMerchantAsync(IyzicoSubMerchantOnboardRequest req, CancellationToken ct = default)
    {
        // Subscriber type'a gore alan zorunlulugu
        var bodyObj = new Dictionary<string, object?>
        {
            ["locale"] = "tr",
            ["conversationId"] = Guid.NewGuid().ToString("N"),
            ["name"] = req.Name,
            ["email"] = req.Email,
            ["gsmNumber"] = req.GsmNumber,
            ["address"] = req.Address,
            ["iban"] = req.Iban,
            ["contactName"] = req.ContactName,
            ["contactSurname"] = req.ContactSurname,
            ["currency"] = req.Currency,
            ["subMerchantExternalId"] = req.SubMerchantExternalId,
            ["subMerchantType"] = req.SubMerchantType
        };

        if (string.Equals(req.SubMerchantType, "PERSONAL", StringComparison.OrdinalIgnoreCase))
        {
            bodyObj["identityNumber"] = req.IdentityNumber ?? "";
        }
        else
        {
            bodyObj["taxOffice"] = req.TaxOffice ?? "";
            bodyObj["taxNumber"] = req.TaxNumber ?? "";
            bodyObj["legalCompanyTitle"] = req.LegalCompanyTitle ?? "";
        }

        var json = JsonSerializer.Serialize(bodyObj);
        var httpRequest = CreateRequest(HttpMethod.Post, "/onboarding/submerchant", json);

        try
        {
            var response = await _http.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;
            if (status == "success")
            {
                var key = root.TryGetProperty("subMerchantKey", out var k) ? k.GetString() : null;
                return string.IsNullOrEmpty(key)
                    ? IyzicoSubMerchantOnboardResult.Fail("subMerchantKey döndü ama boş.")
                    : IyzicoSubMerchantOnboardResult.Ok(key);
            }

            var error = root.TryGetProperty("errorMessage", out var e) ? e.GetString() : "Iyzico sub-merchant olusturma hatasi";
            return IyzicoSubMerchantOnboardResult.Fail(error ?? "Bilinmeyen hata");
        }
        catch (Exception ex)
        {
            return IyzicoSubMerchantOnboardResult.Fail($"Iyzico baglanti hatasi: {ex.Message}");
        }
    }

    // ─── Iyzico Auth Header Olusturma (IYZWSv2 — HMACSHA256) ───
    // BUG2.19: Eski v1 SHA1+PKI string hesabi yanlistı (raw JSON kullanarak "Gecersiz imza" 400).
    // Iyzico v2 spec: signature = HMACSHA256(randomKey + uriPath + jsonBody, secretKey).hex
    // Authorization header: "IYZWSv2 base64(apiKey:KEY&randomKey:R&signature:S)"
    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string jsonBody)
    {
        var randomKey = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{new Random().Next(100000, 999999)}";

        var payload = randomKey + path + jsonBody;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_credentials.SecretKey));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var signatureHex = Convert.ToHexString(signatureBytes).ToLowerInvariant();

        var authString = $"apiKey:{_credentials.ApiKey}&randomKey:{randomKey}&signature:{signatureHex}";
        var authorizationHeader = "IYZWSv2 " + Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

        var request = new HttpRequestMessage(method, path)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        request.Headers.TryAddWithoutValidation("x-iyzi-rnd", randomKey);

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

    public async Task<CheckoutFormResult> InitCheckoutFormAsync(CheckoutFormRequest req, CancellationToken ct = default)
    {
        var priceTxt = req.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        // Sub-merchant split: PS.6/PS.7 — basketItem'a subMerchantKey + subMerchantPrice eklenir.
        var basketItem = new Dictionary<string, object?>
        {
            ["id"] = "SUB01",
            ["name"] = req.Description ?? "Abonelik Odemesi",
            ["category1"] = "Abonelik",
            ["itemType"] = "VIRTUAL",
            ["price"] = priceTxt
        };
        if (!string.IsNullOrWhiteSpace(req.SubMerchantKey) && req.SubMerchantPrice.HasValue)
        {
            basketItem["subMerchantKey"] = req.SubMerchantKey;
            basketItem["subMerchantPrice"] = req.SubMerchantPrice.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }

        var body = new
        {
            locale = "tr",
            conversationId = req.ConversationId,
            price = priceTxt,
            paidPrice = priceTxt,
            currency = req.Currency,
            basketId = req.ConversationId,
            paymentGroup = "SUBSCRIPTION",
            callbackUrl = req.CallbackUrl,
            enabledInstallments = new[] { 1 },
            buyer = new
            {
                id = req.BuyerId,
                name = GetFirstName(req.BuyerName),
                surname = GetLastName(req.BuyerName),
                email = req.BuyerEmail ?? "noreply@corplynk.com",
                identityNumber = "11111111111",
                registrationAddress = "Turkiye",
                ip = req.BuyerIp ?? "127.0.0.1",
                city = "Istanbul",
                country = "Turkey"
            },
            shippingAddress = new { contactName = req.BuyerName ?? "Musteri", city = "Istanbul", country = "Turkey", address = "Turkiye" },
            billingAddress = new { contactName = req.BuyerName ?? "Musteri", city = "Istanbul", country = "Turkey", address = "Turkiye" },
            basketItems = new[] { basketItem }
        };

        var json = JsonSerializer.Serialize(body);
        var httpRequest = CreateRequest(HttpMethod.Post, "/payment/iyzipos/checkoutform/initialize/auth/ecom", json);

        try
        {
            var response = await _http.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<IyzicoCheckoutResponse>(responseBody, JsonOpts);

            if (result?.Status == "success" && !string.IsNullOrEmpty(result.CheckoutFormContent))
                return CheckoutFormResult.Ok(result.CheckoutFormContent, result.Token ?? "");

            return CheckoutFormResult.Fail(result?.ErrorMessage ?? "Checkout form olusturulamadi");
        }
        catch (Exception ex)
        {
            return CheckoutFormResult.Fail($"Iyzico baglanti hatasi: {ex.Message}");
        }
    }

    public async Task<PaymentVerifyResult> VerifyCheckoutFormAsync(string token, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(new { locale = "tr", token });
        var httpRequest = CreateRequest(HttpMethod.Post, "/payment/iyzipos/checkoutform/auth/ecom/detail", json);

        try
        {
            var response = await _http.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<IyzicoPaymentResponse>(responseBody, JsonOpts);

            return result?.Status == "success"
                ? PaymentVerifyResult.Ok(result.PaymentId ?? token, result.PaymentId)
                : PaymentVerifyResult.Fail(result?.ErrorMessage ?? "Checkout dogrulama basarisiz");
        }
        catch (Exception ex)
        {
            return PaymentVerifyResult.Fail($"Iyzico dogrulama hatasi: {ex.Message}");
        }
    }

    // ─── Iyzico Response Models ───

    private class IyzicoPaymentResponse
    {
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
        public string? PaymentId { get; set; }
        public string? ConversationId { get; set; }
    }

    private class IyzicoCheckoutResponse
    {
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CheckoutFormContent { get; set; }
        public string? Token { get; set; }
        public string? PaymentPageUrl { get; set; }
    }
}

public class CheckoutFormRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string ConversationId { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string? BuyerId { get; set; }
    public string? BuyerName { get; set; }
    public string? BuyerEmail { get; set; }
    public string? BuyerIp { get; set; }
    public string? Description { get; set; }

    /// <summary>iyzico Pazaryeri sub-merchant key. Null ise normal direkt-merchant akisi.</summary>
    public string? SubMerchantKey { get; set; }
    /// <summary>Sub-merchant'a aktarilacak tutar (komisyon dusulmus). SubMerchantKey set edildiyse zorunlu.</summary>
    public decimal? SubMerchantPrice { get; set; }
}

public class CheckoutFormResult
{
    public bool Success { get; set; }
    public string? HtmlContent { get; set; }
    public string? Token { get; set; }
    public string? Error { get; set; }

    public static CheckoutFormResult Ok(string html, string token) => new() { Success = true, HtmlContent = html, Token = token };
    public static CheckoutFormResult Fail(string error) => new() { Success = false, Error = error };
}

// ─── Sub-Merchant (Pazaryeri) onboarding ───
public class IyzicoSubMerchantOnboardRequest
{
    /// <summary>"PERSONAL" | "PRIVATE_COMPANY" | "LIMITED_OR_JOINT_STOCK_COMPANY"</summary>
    public string SubMerchantType { get; set; } = "PERSONAL";
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GsmNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactSurname { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public string SubMerchantExternalId { get; set; } = string.Empty;
    /// <summary>PERSONAL icin gerekli (TC kimlik).</summary>
    public string? IdentityNumber { get; set; }
    /// <summary>PRIVATE_COMPANY/LIMITED_OR_JOINT_STOCK_COMPANY icin gerekli.</summary>
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? LegalCompanyTitle { get; set; }
}

public class IyzicoSubMerchantOnboardResult
{
    public bool Success { get; set; }
    public string? SubMerchantKey { get; set; }
    public string? Error { get; set; }

    public static IyzicoSubMerchantOnboardResult Ok(string key) => new() { Success = true, SubMerchantKey = key };
    public static IyzicoSubMerchantOnboardResult Fail(string error) => new() { Success = false, Error = error };
}

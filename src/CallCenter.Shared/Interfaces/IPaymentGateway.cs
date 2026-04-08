namespace CallCenter.Shared.Interfaces;

/// <summary>
/// Online odeme gateway sozlesmesi.
/// Her provider (Iyzico, PayTR, Param) bu arayuzu uygular.
/// Strategy Pattern: Factory, aktif config'e gore dogru provider'i uretir.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Provider tipi ID'si (PaymentProviders.Ids)</summary>
    int ProviderTypeId { get; }

    /// <summary>Odeme baslatir. KK bilgileri ile direkt odeme veya 3DS redirect</summary>
    Task<PaymentInitResult> InitiatePaymentAsync(PaymentRequest request, CancellationToken ct = default);

    /// <summary>Odeme durumunu dogrular (callback/webhook sonrasi)</summary>
    Task<PaymentVerifyResult> VerifyPaymentAsync(string providerTransactionId, CancellationToken ct = default);

    /// <summary>Iade islemi</summary>
    Task<PaymentRefundResult> RefundAsync(string providerTransactionId, decimal amount, CancellationToken ct = default);

    /// <summary>Baglanti testi — credential'larin gecerli oldugunu dogrular</summary>
    Task<(bool Success, string? Error)> TestConnectionAsync(CancellationToken ct = default);
}

/// <summary>Gateway'e gonderilecek odeme bilgileri</summary>
public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";

    // Kart bilgileri
    public string? CardHolderName { get; set; }
    public string? CardNumber { get; set; }
    public string? ExpireMonth { get; set; }
    public string? ExpireYear { get; set; }
    public string? Cvc { get; set; }
    public int Installment { get; set; }

    // Alici bilgileri
    public string? BuyerName { get; set; }
    public string? BuyerEmail { get; set; }
    public string? BuyerPhone { get; set; }
    public string? BuyerIp { get; set; }

    // Islem referansi
    public string ConversationId { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>3DS callback URL (PayTR/Iyzico 3D Secure icin)</summary>
    public string? CallbackUrl { get; set; }
}

/// <summary>Odeme baslama sonucu</summary>
public class PaymentInitResult
{
    public bool Success { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? Error { get; set; }

    /// <summary>3D Secure HTML icerigi (iframe render icin)</summary>
    public string? HtmlContent { get; set; }

    /// <summary>Hosted checkout redirect URL (varsa)</summary>
    public string? RedirectUrl { get; set; }

    /// <summary>true ise islem tamamlandi, false ise 3DS/redirect bekleniyor</summary>
    public bool IsCompleted { get; set; }

    public static PaymentInitResult Ok(string providerTxId, string? paymentId = null) => new()
    {
        Success = true, IsCompleted = true,
        ProviderTransactionId = providerTxId, ProviderPaymentId = paymentId
    };

    public static PaymentInitResult Redirect(string html, string providerTxId) => new()
    {
        Success = true, IsCompleted = false,
        HtmlContent = html, ProviderTransactionId = providerTxId
    };

    public static PaymentInitResult Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>Odeme dogrulama sonucu</summary>
public class PaymentVerifyResult
{
    public bool Success { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? Error { get; set; }

    public static PaymentVerifyResult Ok(string txId, string? paymentId = null) => new()
        { Success = true, ProviderTransactionId = txId, ProviderPaymentId = paymentId };
    public static PaymentVerifyResult Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>Iade sonucu</summary>
public class PaymentRefundResult
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public string? Error { get; set; }

    public static PaymentRefundResult Ok(string refundId) => new() { Success = true, RefundId = refundId };
    public static PaymentRefundResult Fail(string error) => new() { Success = false, Error = error };
}

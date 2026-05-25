using System.Security.Cryptography;
using System.Text;

namespace CallCenter.Api.Services.Payment;

public sealed class IyzicoWebhookSignatureInput
{
    public string? IyziEventType { get; set; }
    public string? PaymentId { get; set; }
    public string? IyziPaymentId { get; set; }
    public string? Token { get; set; }
    public string? PaymentConversationId { get; set; }
    public string? Status { get; set; }
}

public static class IyzicoWebhookSignatureValidator
{
    public static bool Verify(IyzicoWebhookSignatureInput input, string secretKey, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        if (!TryCreateSignature(input, secretKey, out var expected))
            return false;

        var actual = signatureHeader.Trim();
        if (!IsHex(actual) || actual.Length != expected.Length)
            return false;

        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        var actualBytes = Encoding.ASCII.GetBytes(actual.ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    public static bool TryCreateSignature(IyzicoWebhookSignatureInput input, string secretKey, out string signature)
    {
        signature = string.Empty;
        if (string.IsNullOrWhiteSpace(secretKey)
            || string.IsNullOrWhiteSpace(input.IyziEventType)
            || string.IsNullOrWhiteSpace(input.PaymentConversationId)
            || string.IsNullOrWhiteSpace(input.Status))
            return false;

        var isHostedPaymentPage = !string.IsNullOrWhiteSpace(input.Token);
        var message = isHostedPaymentPage
            ? CreateHostedPaymentPageMessage(input, secretKey)
            : CreateDirectMessage(input, secretKey);

        if (message == null)
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message))).ToLowerInvariant();
        return true;
    }

    private static string? CreateDirectMessage(IyzicoWebhookSignatureInput input, string secretKey)
    {
        var paymentId = FirstNonEmpty(input.PaymentId, input.IyziPaymentId);
        return string.IsNullOrWhiteSpace(paymentId)
            ? null
            : secretKey + input.IyziEventType + paymentId + input.PaymentConversationId + input.Status;
    }

    private static string? CreateHostedPaymentPageMessage(IyzicoWebhookSignatureInput input, string secretKey)
    {
        var iyziPaymentId = FirstNonEmpty(input.IyziPaymentId, input.PaymentId);
        return string.IsNullOrWhiteSpace(iyziPaymentId)
            ? null
            : secretKey + input.IyziEventType + iyziPaymentId + input.Token + input.PaymentConversationId + input.Status;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static bool IsHex(string value)
        => value.Length > 0 && value.All(c =>
            c is >= '0' and <= '9'
                or >= 'a' and <= 'f'
                or >= 'A' and <= 'F');
}

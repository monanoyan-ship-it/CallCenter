using CallCenter.Api.Services.Payment;

namespace CallCenter.Tests.Security;

public class IyzicoWebhookSignatureValidatorTests
{
    [Fact]
    public void Verify_accepts_direct_format_signature_v3()
    {
        var input = new IyzicoWebhookSignatureInput
        {
            IyziEventType = "API_AUTH",
            PaymentId = "28157248",
            PaymentConversationId = "conversationId",
            Status = "SUCCESS"
        };

        var valid = IyzicoWebhookSignatureValidator.Verify(
            input,
            "merchant_secret_key",
            "0e071b3d0d41f0804527c8badc185d70e3485f64bf4b1af65fb7e8f05042d10d");

        valid.Should().BeTrue();
    }

    [Fact]
    public void Verify_accepts_hosted_payment_page_signature_v3()
    {
        var input = new IyzicoWebhookSignatureInput
        {
            IyziEventType = "CHECKOUT_FORM_AUTH",
            IyziPaymentId = "28157248",
            Token = "token123",
            PaymentConversationId = "conversationId",
            Status = "SUCCESS"
        };

        var valid = IyzicoWebhookSignatureValidator.Verify(
            input,
            "merchant_secret_key",
            "b084713b2d01147fda95ee0209fbbac410a57f80984f7219ced288fcdeadc2b8");

        valid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("b084713b2d01147fda95ee0209fbbac410a57f80984f7219ced288fcdeadc2b9")]
    public void Verify_rejects_missing_or_invalid_signature(string? signature)
    {
        var input = new IyzicoWebhookSignatureInput
        {
            IyziEventType = "CHECKOUT_FORM_AUTH",
            IyziPaymentId = "28157248",
            Token = "token123",
            PaymentConversationId = "conversationId",
            Status = "SUCCESS"
        };

        var valid = IyzicoWebhookSignatureValidator.Verify(input, "merchant_secret_key", signature);

        valid.Should().BeFalse();
    }
}

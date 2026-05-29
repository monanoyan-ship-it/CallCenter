namespace CallCenter.Shared.DTOs;

public class PaymentCheckoutPreviewDto
{
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
    public string PaymentContext { get; set; } = "all";
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public List<PaymentCheckoutLineDto> Lines { get; set; } = [];
    public string SupportEmail { get; set; } = "info@corplynk.com";
    public string SupportMessage { get; set; } = "Kalemleri onaylamazsaniz bize yazabilirsiniz; 24 saat icinde donus yapariz.";

    public static PaymentCheckoutPreviewDto Fail(string error) => new()
    {
        Success = false,
        Error = error,
        Lines = []
    };
}

public class PaymentCheckoutLineDto
{
    public int BillingPeriodId { get; set; }
    public int BillingKindId { get; set; }
    public string BillingKindName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal ServiceAmount { get; set; }
    public string Description { get; set; } = string.Empty;
}

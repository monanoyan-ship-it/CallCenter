namespace CallCenter.Shared.DTOs;

public class SendVerificationEmailRequest
{
    public string UserName { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string UserName { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class PlatformSendVerificationEmailRequest
{
    public string Email { get; set; } = string.Empty;
}

public class PlatformForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class PlatformResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class PlatformReviewCreateRequest
{
    public string SalonSlug { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? DisplayName { get; set; }
}

public class PlatformReviewDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string SalonName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PlatformPushTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
}

public class PlatformPushTokenDto
{
    public int Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>PS.4 — Salon iyzico Pazaryeri sub-merchant onboarding request</summary>
public class SubMerchantOnboardRequest
{
    /// <summary>"PERSONAL" | "PRIVATE_COMPANY" | "LIMITED_OR_JOINT_STOCK_COMPANY"</summary>
    public string SubMerchantType { get; set; } = "PERSONAL";
    public string Iban { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactSurname { get; set; } = string.Empty;
    /// <summary>PERSONAL icin zorunlu (TC kimlik).</summary>
    public string? IdentityNumber { get; set; }
    public string? LegalCompanyTitle { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
}

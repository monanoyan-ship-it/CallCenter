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

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

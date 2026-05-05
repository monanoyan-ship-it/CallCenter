namespace CallCenter.Shared.DTOs;

// ═══════════════════════════════════════════════════════════════
// E-POSTA ENTEGRASYONU DTO'LAR
// ═══════════════════════════════════════════════════════════════

public class EmailIntegrationListDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int ProviderTypeId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderIcon { get; set; } = string.Empty;
    public string ProviderCss { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string? SenderName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSuccess { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EmailIntegrationDetailDto : EmailIntegrationListDto
{
    public string? LastTestError { get; set; }
}

/// <summary>SMTP/Yandex icin manuel kayit</summary>
public class CreateEmailIntegrationRequest
{
    public int ProviderTypeId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string? SenderName { get; set; }
    public bool IsDefault { get; set; }
    public Dictionary<string, string> Credentials { get; set; } = new();
}

public class UpdateEmailIntegrationRequest
{
    public string? DisplayName { get; set; }
    public string? SenderName { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDefault { get; set; }
    public Dictionary<string, string>? Credentials { get; set; }
}

public class EmailTestSendRequest
{
    public string ToAddress { get; set; } = string.Empty;
}

/// <summary>IEmailSendService tuketicileri icin istek DTO'su</summary>
public class EmailSendRequest
{
    public int CustomerId { get; set; }
    public int? IntegrationId { get; set; }
    public string ToAddress { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? PlainTextBody { get; set; }
    public string? ReplyTo { get; set; }
    public List<EmailAttachmentDto> Attachments { get; set; } = new();
}

public class EmailAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public class EmailSendResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? MessageId { get; set; }
}

// ═══ OAuth DTO'lar ═══

public class EmailOAuthUrlResponse
{
    public string AuthUrl { get; set; } = string.Empty;
}

public class EmailOAuthExchangeRequest
{
    public string Code { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsDefault { get; set; }
}

public class EmailOAuthExchangeResponse
{
    public bool Success { get; set; }
    public string? Email { get; set; }
    public string? Error { get; set; }
}

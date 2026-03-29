namespace CallCenter.Shared.Entities;

/// <summary>D1 - Dijital onay/rizalik formu tanimi</summary>
public class SlnConsentForm
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Title { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public bool RequireSignature { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>D1 - Musteri tarafindan imzalanmis form</summary>
public class SlnClientConsent
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public SlnConsentForm? Form { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public string? SignatureData { get; set; }
    public string? IpAddress { get; set; }
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;
}

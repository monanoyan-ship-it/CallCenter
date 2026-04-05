namespace CallCenter.Shared.DTOs;

public class PlatformEmailTemplateDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public string? ProductType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string? AvailablePlaceholders { get; set; }
    public string Language { get; set; } = "tr";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PlatformEmailTemplateCreateDto
{
    public string EventKey { get; set; } = string.Empty;
    public string? ProductType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public string? AvailablePlaceholders { get; set; }
    public string Language { get; set; } = "tr";
}

public class PlatformEmailTemplateUpdateDto
{
    public string? ProductType { get; set; }
    public string? Subject { get; set; }
    public string? HtmlBody { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
    public string? AvailablePlaceholders { get; set; }
    public string? Language { get; set; }
}

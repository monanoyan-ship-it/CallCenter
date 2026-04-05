namespace CallCenter.Shared.DTOs;

// ─── Event DTO ───

public class PlatformEmailEventDto
{
    public int Id { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public string? ProductType { get; set; }
    public string? Description { get; set; }
    public string? AvailablePlaceholders { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PlatformEmailTemplateDto> Templates { get; set; } = new();
}

public class PlatformEmailEventCreateDto
{
    public string EventKey { get; set; } = string.Empty;
    public string? ProductType { get; set; }
    public string? Description { get; set; }
    public string? AvailablePlaceholders { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PlatformEmailEventUpdateDto
{
    public string? ProductType { get; set; }
    public string? Description { get; set; }
    public string? AvailablePlaceholders { get; set; }
    public bool? IsActive { get; set; }
}

// ─── Template DTO (dil bazli) ───

public class PlatformEmailTemplateDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Language { get; set; } = "tr";
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PlatformEmailTemplateCreateDto
{
    public string Language { get; set; } = "tr";
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class PlatformEmailTemplateUpdateDto
{
    public string? Subject { get; set; }
    public string? HtmlBody { get; set; }
    public bool? IsActive { get; set; }
}

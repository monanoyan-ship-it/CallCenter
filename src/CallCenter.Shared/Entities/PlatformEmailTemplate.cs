namespace CallCenter.Shared.Entities;

/// <summary>
/// Platform email taslagi (dil bazli).
/// Her taslak bir PlatformEmailEvent'e aittir.
/// </summary>
public class PlatformEmailTemplate
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public PlatformEmailEvent? Event { get; set; }

    /// <summary>Dil kodu: "tr", "en"</summary>
    public string Language { get; set; } = "tr";

    /// <summary>Email basligi. Placeholder destekler: {{SalonAdi}}</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>HTML govde. WYSIWYG editorle duzenlenir.</summary>
    public string HtmlBody { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

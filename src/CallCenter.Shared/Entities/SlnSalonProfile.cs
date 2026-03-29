namespace CallCenter.Shared.Entities;

/// <summary>
/// Salonun herkese acik profil bilgileri
/// </summary>
public class SlnSalonProfile
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? InstagramHandle { get; set; }
    public string? FacebookUrl { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }

    /// <summary>Calisma saatleri JSON (orn: {"mon":"09:00-19:00","tue":"09:00-19:00",...})</summary>
    public string? WorkingHoursJson { get; set; }

    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

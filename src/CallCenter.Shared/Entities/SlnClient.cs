namespace CallCenter.Shared.Entities;

public class SlnClient
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Phone2 { get; set; }
    public string? Email { get; set; }
    public int? GenderId { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime? MarriageDate { get; set; }
    public string? Occupation { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }

    // Sac/guzellik bilgileri
    public string? HairColor { get; set; }
    public int? WhiteRatioPercent { get; set; }
    public string? SkinType { get; set; }
    public string? Notes { get; set; }
    public bool IsFavorite { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<SlnFormula> Formulas { get; set; } = [];
    public ICollection<SlnClientPhoto> Photos { get; set; } = [];
    public ICollection<SlnAppointment> Appointments { get; set; } = [];
    public ICollection<SlnInvoice> Invoices { get; set; } = [];
}

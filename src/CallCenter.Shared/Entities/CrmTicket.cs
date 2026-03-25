namespace CallCenter.Shared.Entities;

/// <summary>
/// CRM destek talebi. Musteriye ait, bir CrmContact ile iliskili.
/// </summary>
public class CrmTicket
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>TicketStatuses TypeItem referansi</summary>
    public int StatusId { get; set; } = 1; // Open

    /// <summary>TicketPriorities TypeItem referansi</summary>
    public int PriorityId { get; set; } = 2; // Medium

    /// <summary>Kategori</summary>
    public int? CategoryId { get; set; }
    public CrmTicketCategory? Category { get; set; }

    /// <summary>Ilgili kisi</summary>
    public int? CrmContactId { get; set; }
    public CrmContact? CrmContact { get; set; }

    /// <summary>Atanan personel</summary>
    public int? AssignedToPersonnelId { get; set; }
    public CustomerPersonnel? AssignedToPersonnel { get; set; }

    /// <summary>Olusturan personel</summary>
    public int CreatedByPersonnelId { get; set; }
    public CustomerPersonnel CreatedByPersonnel { get; set; } = null!;

    /// <summary>Musteri bazli izolasyon</summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<CrmActivity> Activities { get; set; } = new List<CrmActivity>();
    public ICollection<CrmTicketComment> Comments { get; set; } = new List<CrmTicketComment>();
}

namespace CallCenter.Shared.Entities;

/// <summary>
/// CRM gorevi. Personele atanir, bir CrmContact/Ticket/Deal ile iliskilendirilebilir.
/// </summary>
public class CrmTask
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>CrmTaskStatuses TypeItem referansi</summary>
    public int StatusId { get; set; } = 1; // Todo

    /// <summary>Son tarih</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Ilgili kisi</summary>
    public int? CrmContactId { get; set; }
    public CrmContact? CrmContact { get; set; }

    /// <summary>Ilgili talep (opsiyonel)</summary>
    public int? TicketId { get; set; }
    public CrmTicket? Ticket { get; set; }

    /// <summary>Ilgili firsat (opsiyonel)</summary>
    public int? DealId { get; set; }
    public CrmDeal? Deal { get; set; }

    /// <summary>Atanan personel</summary>
    public int AssignedToPersonnelId { get; set; }
    public CustomerPersonnel AssignedToPersonnel { get; set; } = null!;

    /// <summary>Olusturan personel</summary>
    public int CreatedByPersonnelId { get; set; }
    public CustomerPersonnel CreatedByPersonnel { get; set; } = null!;

    /// <summary>Musteri bazli izolasyon</summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

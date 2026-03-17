namespace CallCenter.Shared.Entities;

/// <summary>
/// Talep kategorisi. Firma admini olusturur (Teknik Destek, Satis, Sikayet vb.)
/// </summary>
public class CrmTicketCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public ICollection<CrmTicket> Tickets { get; set; } = new List<CrmTicket>();
}

/// <summary>
/// Talep yorumu / ilerleme notu.
/// </summary>
public class CrmTicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public CrmTicket Ticket { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    /// <summary>Dahili not mu (musteri goremez) yoksa dis yorum mu</summary>
    public bool IsInternal { get; set; }

    public int CreatedByPersonnelId { get; set; }
    public CustomerPersonnel CreatedByPersonnel { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

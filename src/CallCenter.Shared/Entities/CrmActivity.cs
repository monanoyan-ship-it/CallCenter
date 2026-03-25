namespace CallCenter.Shared.Entities;

/// <summary>
/// CRM etkilesim kaydi. Arama, e-posta, toplanti, not vb.
/// CrmContact, Ticket veya Deal ile iliskilendirilebilir.
/// </summary>
public class CrmActivity
{
    public int Id { get; set; }

    /// <summary>ActivityTypes TypeItem referansi</summary>
    public int TypeId { get; set; } = 1; // Call

    public string? Summary { get; set; }
    public string? Detail { get; set; }

    /// <summary>Ilgili kisi</summary>
    public int? CrmContactId { get; set; }
    public CrmContact? CrmContact { get; set; }

    /// <summary>Ilgili talep (opsiyonel)</summary>
    public int? TicketId { get; set; }
    public CrmTicket? Ticket { get; set; }

    /// <summary>Ilgili firsat (opsiyonel)</summary>
    public int? DealId { get; set; }
    public CrmDeal? Deal { get; set; }

    /// <summary>Ilgili cagri kaydi (opsiyonel - telefon etkilesimi ise)</summary>
    public int? CallRecordId { get; set; }
    public CallRecord? CallRecord { get; set; }

    /// <summary>Islem yapan personel</summary>
    public int PersonnelId { get; set; }
    public CustomerPersonnel Personnel { get; set; } = null!;

    /// <summary>Musteri bazli izolasyon</summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

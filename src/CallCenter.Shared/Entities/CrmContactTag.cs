namespace CallCenter.Shared.Entities;

/// <summary>
/// CrmContact etiketi tanimi (VIP, Kara Liste, Potansiyel vb.)
/// Firma admini olusturur.
/// </summary>
public class CrmContactTag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Etiket rengi (hex: #FF5733)</summary>
    public string? Color { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public ICollection<CrmContactTagLink> CrmContactLinks { get; set; } = new List<CrmContactTagLink>();
}

/// <summary>
/// CrmContact - Etiket iliskisi (many-to-many).
/// </summary>
public class CrmContactTagLink
{
    public int Id { get; set; }

    public int CrmContactId { get; set; }
    public CrmContact CrmContact { get; set; } = null!;

    public int TagId { get; set; }
    public CrmContactTag Tag { get; set; } = null!;
}

namespace CallCenter.Shared.Entities;

/// <summary>
/// Contact etiketi tanimi (VIP, Kara Liste, Potansiyel vb.)
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

    public ICollection<CrmContactTagLink> ContactLinks { get; set; } = new List<CrmContactTagLink>();
}

/// <summary>
/// Contact - Etiket iliskisi (many-to-many).
/// </summary>
public class CrmContactTagLink
{
    public int Id { get; set; }

    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;

    public int TagId { get; set; }
    public CrmContactTag Tag { get; set; } = null!;
}

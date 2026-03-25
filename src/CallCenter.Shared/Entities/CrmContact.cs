namespace CallCenter.Shared.Entities;

/// <summary>
/// Rehber kaydi. LDAP/CSV/Manuel olarak eklenir.
/// Kullaniciya veya musteriye ait olabilir.
/// </summary>
public class CrmContact
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Kisi adi</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Telefon numarasi (birincil)</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Ikinci telefon (opsiyonel)</summary>
    public string? PhoneNumber2 { get; set; }

    /// <summary>E-posta</summary>
    public string? Email { get; set; }

    /// <summary>Sirket / Kurum</summary>
    public string? Company { get; set; }

    /// <summary>Departman</summary>
    public string? Department { get; set; }

    /// <summary>Unvan</summary>
    public string? Title { get; set; }

    /// <summary>Not</summary>
    public string? Notes { get; set; }

    /// <summary>Kaynak: Manual, LDAP, CSV</summary>
    public int SourceId { get; set; } = 1; // CrmContactSources.Ids.Manual

    /// <summary>LDAP Distinguished Name (LDAP kaynakliysa)</summary>
    public string? LdapDn { get; set; }

    /// <summary>Bu rehber kaydinin sahibi kullanici (null ise paylasilmis)</summary>
    public int? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }

    /// <summary>Musteri bazli izolasyon</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Favori mi</summary>
    public bool IsFavorite { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

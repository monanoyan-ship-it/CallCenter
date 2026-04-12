namespace CallCenter.Shared.Entities;

/// <summary>
/// Salon subesi. Her firma birden fazla subeye sahip olabilir.
/// BranchId null olan veriler = merkez/tek sube (geriye uyumluluk).
/// </summary>
public class SlnBranch
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Sube yoneticisi</summary>
    public int? ManagerPersonnelId { get; set; }
    public CustomerPersonnel? ManagerPersonnel { get; set; }

    /// <summary>Calisma saatleri JSON (orn: {"mon":"09:00-19:00",...})</summary>
    public string? WorkingHoursJson { get; set; }

    /// <summary>Konum</summary>
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Sube logosu/fotografi</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>Ana sube mi? (firma basina bir tane)</summary>
    public bool IsHeadquarter { get; set; }

    /// <summary>Sube profil adresi (corplynk.com/salon/{slug})</summary>
    public string? Slug { get; set; }

    /// <summary>Google Maps embed/link</summary>
    public string? GoogleMapsUrl { get; set; }

    // ─── Fatura Bilgileri ───
    /// <summary>Ticari unvan (default: Customer.Name)</summary>
    public string? CompanyTitle { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? MersisNo { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Aktif edilme tarihi (faturalama icin — ne zamandan itibaren aktif)</summary>
    public DateTime? ActivatedAt { get; set; }

    /// <summary>Pasif yapilma tarihi (faturalama icin — ne zamana kadar aktifti)</summary>
    public DateTime? DeactivatedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Subeye ait personeller
    public ICollection<CustomerPersonnel> Personnel { get; set; } = [];
}

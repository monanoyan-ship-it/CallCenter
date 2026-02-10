namespace CallCenter.Shared.Entities;

public class CustomerPersonnel
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Her CustomerPersonnel bir User'a bağlı (login için)
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Hangi müşteriye ait
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Kullanici tipi (sablon yetki seti) — opsiyonel
    public int? UserTypeId { get; set; }
    public CustomerUserType? UserType { get; set; }

    // Organizasyon birimi (opsiyonel)
    public int? OrganizationUnitId { get; set; }
    public CustomerOrganizationUnit? OrganizationUnit { get; set; }

    // Ust yonetici (self-reference, opsiyonel)
    public int? ReportsToPersonnelId { get; set; }
    public CustomerPersonnel? ReportsToPersonnel { get; set; }
    public ICollection<CustomerPersonnel> Subordinates { get; set; } = new List<CustomerPersonnel>();

    // Dinamik yetkiler (CustomerPersonnelPermission tablosu)
    public ICollection<CustomerPersonnelPermission> Permissions { get; set; } = new List<CustomerPersonnelPermission>();
}

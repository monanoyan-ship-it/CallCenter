namespace CallCenter.Shared.Entities;

public class CustomerPersonnel
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsCustomerAdmin { get; set; } // Musteri yoneticisi — tum izinlere sahip, sadece system admin atayabilir
    public string? PhotoUrl { get; set; }
    public string? Specialty { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Her CustomerPersonnel bir User'a bağlı (login için)
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Hangi müşteriye ait
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Sabit firma rolu (CustomerRoles TypeItem referansi, FK degil)
    public int CustomerRoleId { get; set; }

    // Sube (opsiyonel — null = merkez/tum subeler)
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    // Organizasyon birimi (opsiyonel)
    public int? OrganizationUnitId { get; set; }
    public CustomerOrganizationUnit? OrganizationUnit { get; set; }

    // Ust yonetici (self-reference, opsiyonel)
    public int? ReportsToPersonnelId { get; set; }
    public CustomerPersonnel? ReportsToPersonnel { get; set; }
    public ICollection<CustomerPersonnel> Subordinates { get; set; } = new List<CustomerPersonnel>();

    // Ek organizasyon atamalari (junction - coka-cok)
    public ICollection<CustomerPersonnelOrganizationUnit> OrganizationUnits { get; set; } = new List<CustomerPersonnelOrganizationUnit>();
}

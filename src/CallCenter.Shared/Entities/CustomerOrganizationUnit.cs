namespace CallCenter.Shared.Entities;

public class CustomerOrganizationUnit
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int TypeId { get; set; } // OrganizationUnitTypes TypeItem ref
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Hangi musteriye ait
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Self-referencing: ust birim (null = kok)
    public int? ParentId { get; set; }
    public CustomerOrganizationUnit? Parent { get; set; }

    // Navigation collections
    public ICollection<CustomerOrganizationUnit> Children { get; set; } = new List<CustomerOrganizationUnit>();
    public ICollection<CustomerPersonnel> Personnel { get; set; } = new List<CustomerPersonnel>(); // Birincil org (OrganizationUnitId FK)
    public ICollection<CustomerPersonnelOrganizationUnit> PersonnelAssignments { get; set; } = new List<CustomerPersonnelOrganizationUnit>(); // Ek org atamalari (junction)
    public ICollection<Queue> Queues { get; set; } = new List<Queue>();
    public ICollection<SipAccount> SipAccounts { get; set; } = new List<SipAccount>();
}

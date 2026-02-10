namespace CallCenter.Shared.Entities;

public class CustomerUserType
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Hangi musteriye ait
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Bu tipe atanmis yetkiler (sablon)
    public ICollection<CustomerUserTypePermission> Permissions { get; set; } = new List<CustomerUserTypePermission>();

    // Bu tipteki personeller
    public ICollection<CustomerPersonnel> Personnel { get; set; } = new List<CustomerPersonnel>();
}

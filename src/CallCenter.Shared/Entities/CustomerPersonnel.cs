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

    // Dinamik yetkiler (CustomerPersonnelPermission tablosu)
    public ICollection<CustomerPersonnelPermission> Permissions { get; set; } = new List<CustomerPersonnelPermission>();
}

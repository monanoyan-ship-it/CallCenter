namespace CallCenter.Windows.Models;

/// <summary>
/// Yerel rehber kaydi.
/// MicroSIP'teki gibi lokal telefon defteri.
/// </summary>
public class Contact
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Email { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

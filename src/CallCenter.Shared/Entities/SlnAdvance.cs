namespace CallCenter.Shared.Entities;

public class SlnAdvance
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    public decimal Amount { get; set; }
    public DateTime AdvanceDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

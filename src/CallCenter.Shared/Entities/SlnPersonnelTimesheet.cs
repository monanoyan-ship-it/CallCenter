namespace CallCenter.Shared.Entities;

public class SlnPersonnelTimesheet
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? ClockInAt { get; set; }
    public DateTime? ClockOutAt { get; set; }
    public int BreakMinutes { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

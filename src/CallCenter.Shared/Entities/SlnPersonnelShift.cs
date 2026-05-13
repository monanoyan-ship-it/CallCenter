namespace CallCenter.Shared.Entities;

public class SlnPersonnelShift
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }
    public DateTime ShiftDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakMinutes { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

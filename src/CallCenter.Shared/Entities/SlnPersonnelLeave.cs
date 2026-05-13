namespace CallCenter.Shared.Entities;

public class SlnPersonnelLeave
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }
    public int LeaveTypeId { get; set; }
    public int StatusId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
    public int? ReviewedByPersonnelId { get; set; }
    public CustomerPersonnel? ReviewedByPersonnel { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

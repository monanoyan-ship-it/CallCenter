namespace CallCenter.Shared.Entities;

public class SlnCashClosing
{
    public int Id { get; set; }
    public int RegisterId { get; set; }
    public SlnCashRegister? Register { get; set; }

    public DateTime ClosingDate { get; set; }
    public decimal SystemTotal { get; set; }
    public decimal CountedTotal { get; set; }
    public decimal Difference { get; set; }
    public string? Notes { get; set; }

    public int? ClosedByPersonnelId { get; set; }
    public CustomerPersonnel? ClosedByPersonnel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

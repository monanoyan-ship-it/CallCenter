namespace CallCenter.Shared.Entities;

/// <summary>Aylik maas bordrosu</summary>
public class SlnPayroll
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal ServiceCommission { get; set; }
    public decimal ProductCommission { get; set; }
    public decimal TotalAdvance { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetPay { get; set; }
    public string? Notes { get; set; }
    public bool IsFinalized { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

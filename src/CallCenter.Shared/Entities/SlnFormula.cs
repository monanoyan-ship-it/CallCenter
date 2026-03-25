namespace CallCenter.Shared.Entities;

public class SlnFormula
{
    public int Id { get; set; }
    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public string FormulaText { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
    public string? OxidantRatio { get; set; }
    public string? ApplicationNotes { get; set; }

    public int? AppliedByPersonnelId { get; set; }
    public CustomerPersonnel? AppliedByPersonnel { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

namespace CallCenter.Shared.Entities;

public class SlnServiceComboItem
{
    public int Id { get; set; }
    public int ComboId { get; set; }
    public SlnServiceCombo? Combo { get; set; }

    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }
    public int SortOrder { get; set; }
}

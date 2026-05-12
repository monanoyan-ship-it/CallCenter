namespace CallCenter.Shared.Entities;

public class SlnServiceResourceRequirement
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public int ResourceId { get; set; }
    public SlnResource? Resource { get; set; }

    public int QuantityRequired { get; set; } = 1;
}

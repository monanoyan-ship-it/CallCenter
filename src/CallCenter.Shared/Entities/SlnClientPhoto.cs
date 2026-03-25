namespace CallCenter.Shared.Entities;

public class SlnClientPhoto
{
    public int Id { get; set; }
    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public string FilePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;
}

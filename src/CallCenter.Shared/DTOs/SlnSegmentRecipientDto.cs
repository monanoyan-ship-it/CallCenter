namespace CallCenter.Shared.DTOs;

public class SlnSegmentRecipientDto
{
    public int ClientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

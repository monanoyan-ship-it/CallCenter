namespace CallCenter.Shared.Entities;

/// <summary>
/// Konferans odasi. Medya sunucusu (Asterisk/FreeSWITCH) entegre edildiginde
/// gercek ses karistirma yapilacak; su an sadece DB durum yonetimi.
/// </summary>
public class ConferenceRoom
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int StatusId { get; set; } = 1; // ConferenceStatuses.Ids.Active
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int MaxParticipants { get; set; } = 10;

    /// <summary>Medya sunucusu oda kimliği (Asterisk conf-bridge ID vs.)</summary>
    public string? MediaServerRoomId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public ICollection<ConferenceParticipant> Participants { get; set; } = new List<ConferenceParticipant>();
}

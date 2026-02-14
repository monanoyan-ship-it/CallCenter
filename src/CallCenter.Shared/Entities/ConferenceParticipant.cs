namespace CallCenter.Shared.Entities;

/// <summary>
/// Konferans katilimcisi. Agent veya dis numara olabilir.
/// </summary>
public class ConferenceParticipant
{
    public int Id { get; set; }
    public int ConferenceRoomId { get; set; }
    public ConferenceRoom? ConferenceRoom { get; set; }

    /// <summary>Agent ise UserId dolu</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Dis numara ise (agent degilse)</summary>
    public string? ExternalNumber { get; set; }

    /// <summary>Katilimci rolu: Host, Participant, Listener</summary>
    public int RoleId { get; set; } = 2; // ConferenceParticipantRoles.Ids.Participant

    /// <summary>Katilimci durumu: Invited, Joining, Joined, Left, Muted, Kicked</summary>
    public int StatusId { get; set; } = 1; // ConferenceParticipantStatuses.Ids.Invited

    public bool IsMuted { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
}

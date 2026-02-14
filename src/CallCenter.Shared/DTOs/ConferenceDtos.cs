using System.ComponentModel.DataAnnotations;

namespace CallCenter.Shared.DTOs;

public class ConferenceRoomDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public int? CustomerId { get; set; }
    public int MaxParticipants { get; set; }
    public int ParticipantCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public List<ConferenceParticipantDto> Participants { get; set; } = new();
}

public class ConferenceParticipantDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? ExternalNumber { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsMuted { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
}

public class CreateConferenceRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public int? CustomerId { get; set; }

    [Range(2, 20)]
    public int MaxParticipants { get; set; } = 10;
}

public class AddParticipantRequest
{
    public int? UserId { get; set; }

    [StringLength(200)]
    public string? ExternalNumber { get; set; }

    [Range(1, 3)]
    public int RoleId { get; set; } = 2; // Participant
}

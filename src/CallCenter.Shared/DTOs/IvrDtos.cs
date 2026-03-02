namespace CallCenter.Shared.DTOs;

// ═══════════════════════════════════════════
// Greeting Message DTOs
// ═══════════════════════════════════════════

public class GreetingMessageDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? AudioFileName { get; set; }
    public int? DurationSeconds { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateGreetingMessageRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "business_hours";
}

public class UpdateGreetingMessageRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public bool? IsActive { get; set; }
}

// ═══════════════════════════════════════════
// IVR Menu DTOs
// ═══════════════════════════════════════════

public class IvrMenuDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? GreetingMessageId { get; set; }
    public string? GreetingMessageName { get; set; }
    public int MaxRetries { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool IsActive { get; set; }
    public List<IvrMenuOptionDto> Options { get; set; } = new();
}

public class IvrMenuOptionDto
{
    public int Id { get; set; }
    public string Digit { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public int? TargetQueueId { get; set; }
    public string? TargetQueueName { get; set; }
    public string? TargetExtension { get; set; }
    public int? TargetIvrMenuId { get; set; }
    public string? TargetIvrMenuName { get; set; }
    public int? TargetGreetingMessageId { get; set; }
    public string? Label { get; set; }
    public int SortOrder { get; set; }
}

public class CreateIvrMenuRequest
{
    public string Name { get; set; } = string.Empty;
    public int? GreetingMessageId { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 5;
    public List<CreateIvrMenuOptionRequest> Options { get; set; } = new();
}

public class CreateIvrMenuOptionRequest
{
    public string Digit { get; set; } = string.Empty;
    public string ActionType { get; set; } = "transfer_queue";
    public int? TargetQueueId { get; set; }
    public string? TargetExtension { get; set; }
    public int? TargetIvrMenuId { get; set; }
    public int? TargetGreetingMessageId { get; set; }
    public string? Label { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateIvrMenuRequest
{
    public string? Name { get; set; }
    public int? GreetingMessageId { get; set; }
    public int? MaxRetries { get; set; }
    public int? TimeoutSeconds { get; set; }
    public bool? IsActive { get; set; }
    public List<CreateIvrMenuOptionRequest>? Options { get; set; }
}

// ═══════════════════════════════════════════
// Hold Music DTOs
// ═══════════════════════════════════════════

public class HoldMusicDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? QueueId { get; set; }
    public string? QueueName { get; set; }
    public string? AudioFileName { get; set; }
    public int? DurationSeconds { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class CreateHoldMusicRequest
{
    public string Name { get; set; } = string.Empty;
    public int? QueueId { get; set; }
    public bool IsDefault { get; set; }
}

// ═══════════════════════════════════════════
// Business Hours DTOs
// ═══════════════════════════════════════════

public class BusinessHoursDto
{
    public int Id { get; set; }
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkday { get; set; }
}

public class SetBusinessHoursRequest
{
    public List<BusinessHoursDayRequest> Days { get; set; } = new();
}

public class BusinessHoursDayRequest
{
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkday { get; set; } = true;
}

// ═══════════════════════════════════════════
// Holiday DTOs
// ═══════════════════════════════════════════

public class HolidayDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? GreetingMessageId { get; set; }
    public string? GreetingMessageName { get; set; }
    public bool IsRecurring { get; set; }
}

public class CreateHolidayRequest
{
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? GreetingMessageId { get; set; }
    public bool IsRecurring { get; set; }
}

// ═══════════════════════════════════════════
// Incoming Call Pipeline — Runtime Config
// ═══════════════════════════════════════════

/// <summary>
/// Musterinin gelen arama yapilandirmasi.
/// Windows uygulamasi bu bilgiyle gelen aramayi yonetir.
/// </summary>
public class IncomingCallConfigDto
{
    public bool IsWithinBusinessHours { get; set; }
    public bool IsHoliday { get; set; }
    public string? HolidayName { get; set; }

    /// <summary>Calinacak karsilama mesaji dosya yolu (null = yok)</summary>
    public string? GreetingAudioPath { get; set; }

    /// <summary>IVR menu aktif mi</summary>
    public bool HasIvrMenu { get; set; }

    /// <summary>Aktif IVR menu ID (varsa)</summary>
    public int? IvrMenuId { get; set; }

    /// <summary>IVR menu secenekleri</summary>
    public List<IvrMenuOptionDto>? IvrOptions { get; set; }

    /// <summary>Bekleme muzigi dosya yolu</summary>
    public string? HoldMusicAudioPath { get; set; }
}

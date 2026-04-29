namespace CallCenter.Shared.DTOs;

// ═══ Auth ═══

public class PlatformRegisterDto
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class PlatformLoginDto
{
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class PlatformAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public PlatformUserDto User { get; set; } = null!;
}

// ═══ User ═══

public class PlatformUserDto
{
    public Guid Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public int SalonCount { get; set; }
    public int BillingType { get; set; }
    public string? BillingFullName { get; set; }
    public string? BillingCompanyName { get; set; }
    public string? BillingTaxOffice { get; set; }
    public string? BillingTaxNumber { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingDistrict { get; set; }
    public string? BillingPostalCode { get; set; }
}

public class PlatformUserUpdateDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}

public class PlatformChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class PlatformBillingUpdateDto
{
    public int BillingType { get; set; } = 1;
    public string? BillingFullName { get; set; }
    public string? BillingCompanyName { get; set; }
    public string? BillingTaxOffice { get; set; }
    public string? BillingTaxNumber { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingDistrict { get; set; }
    public string? BillingPostalCode { get; set; }
}

// ═══ Salon Uyelik ═══

public class PlatformSalonDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string SalonName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class PlatformJoinSalonDto
{
    public int CustomerId { get; set; }
}

// ═══ Randevu ═══

public class PlatformAppointmentDto
{
    public int Id { get; set; }
    public string SalonName { get; set; } = string.Empty;
    public string? SalonLogoUrl { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? PersonnelName { get; set; }
    public List<string> ServiceNames { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public bool IsPrepaid { get; set; }
    public decimal PrepaidAmount { get; set; }
}

public class PlatformCreateAppointmentDto
{
    public int CustomerId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public int? PersonnelId { get; set; }
    public List<int> ServiceIds { get; set; } = new();
    public string? Notes { get; set; }
}

// ═══ Sadakat ═══

public class PlatformLoyaltyDto
{
    public string SalonName { get; set; } = string.Empty;
    public decimal CurrentPoints { get; set; }
    public decimal TotalEarned { get; set; }
    public string? MembershipPlanName { get; set; }
    public decimal? MembershipDiscount { get; set; }
    public List<PlatformGiftCardDto> GiftCards { get; set; } = new();
}

public class PlatformGiftCardDto
{
    public string Code { get; set; } = string.Empty;
    public decimal RemainingBalance { get; set; }
    public decimal OriginalAmount { get; set; }
    public bool IsActive { get; set; }
}

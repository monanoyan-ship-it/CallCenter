using System.ComponentModel.DataAnnotations;
using CallCenter.Shared.Enums;

namespace CallCenter.Shared.DTOs;

// ═══════════════════════════════════════════════════════════════
// PORTAL PERSONNEL DTO'LARI
// ═══════════════════════════════════════════════════════════════

public class PortalPersonnelListDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int CustomerRoleId { get; set; }
    public string? CustomerRoleName { get; set; }
    public int? OrganizationUnitId { get; set; }
    public string? OrganizationUnitName { get; set; }
    public int? ReportsToPersonnelId { get; set; }
    public string? ReportsToPersonnelName { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public List<int>? SkillServiceIds { get; set; }

    public string? PhotoUrl { get; set; }

    // Public gorunurluk
    public bool PublicVisible { get; set; }
    public bool PublicShowFullName { get; set; }
    public bool PublicShowPhoto { get; set; }
    public bool PublicShowTitle { get; set; }
    public bool PublicShowSpecialty { get; set; }

    /// <summary>JSON: { "mon":"09:00-18:00","tue":"closed",... }. Null/bos -> sube saatleri kullanilir.</summary>
    public string? WorkingHoursJson { get; set; }
}

public class PortalPersonnelCreateDto
{
    [Required(ErrorMessage = "Kullanici adi zorunludur.")]
    [MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre zorunludur.")]
    [MinLength(8, ErrorMessage = "Sifre en az 8 karakter olmalidir.")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol secimi zorunludur.")]
    public int CustomerRoleId { get; set; }
    public int? OrganizationUnitId { get; set; }
    public int? ReportsToPersonnelId { get; set; }
    public int? BranchId { get; set; }
    public List<int>? SkillServiceIds { get; set; }
    public bool IsActive { get; set; } = true;

    // Public gorunurluk (varsayilan: true)
    public bool PublicVisible { get; set; } = true;
    public bool PublicShowFullName { get; set; } = true;
    public bool PublicShowPhoto { get; set; } = true;
    public bool PublicShowTitle { get; set; } = true;
    public bool PublicShowSpecialty { get; set; } = true;

    public string? WorkingHoursJson { get; set; }
}

public class PortalPersonnelUpdateDto
{
    [MaxLength(50)]
    public string? UserName { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MinLength(8, ErrorMessage = "Sifre en az 8 karakter olmalidir.")]
    public string? Password { get; set; }

    [MaxLength(100)]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Rol secimi zorunludur.")]
    public int CustomerRoleId { get; set; }
    public int? OrganizationUnitId { get; set; }
    public int? ReportsToPersonnelId { get; set; }
    public int? BranchId { get; set; }
    public List<int>? SkillServiceIds { get; set; }

    public bool IsActive { get; set; } = true;
    public string? PhotoUrl { get; set; }

    // Public gorunurluk
    public bool PublicVisible { get; set; } = true;
    public bool PublicShowFullName { get; set; } = true;
    public bool PublicShowPhoto { get; set; } = true;
    public bool PublicShowTitle { get; set; } = true;
    public bool PublicShowSpecialty { get; set; } = true;

    public string? WorkingHoursJson { get; set; }
}

public class PortalPersonnelPasswordResetDto
{
    [Required(ErrorMessage = "Sifre zorunludur.")]
    [MinLength(8, ErrorMessage = "Sifre en az 8 karakter olmalidir.")]
    public string Password { get; set; } = string.Empty;
}

public class PortalPersonnelOpsDto
{
    public List<PortalPersonnelShiftDto> Shifts { get; set; } = new();
    public List<PortalPersonnelLeaveDto> Leaves { get; set; } = new();
    public List<PortalPersonnelTimesheetDto> Timesheets { get; set; } = new();
    public List<PortalPayrollDto> Payrolls { get; set; } = new();
    public List<PortalAdvanceDto> Advances { get; set; } = new();
    public List<TypeItemDto> LeaveTypes { get; set; } = new();
    public List<TypeItemDto> LeaveStatuses { get; set; } = new();
}

public class TypeItemDto
{
    public int Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? ColorClass { get; set; }
}

public class PortalPersonnelShiftDto
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public DateTime ShiftDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int BreakMinutes { get; set; }
    public string? Notes { get; set; }
}

public class PortalPersonnelShiftUpsertDto
{
    public int PersonnelId { get; set; }
    public DateTime ShiftDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int BreakMinutes { get; set; }
    public string? Notes { get; set; }
}

public class PortalPersonnelLeaveDto
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
}

public class PortalPersonnelLeaveCreateDto
{
    public int PersonnelId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
}

public class PortalPersonnelLeaveStatusDto
{
    public int StatusId { get; set; }
}

public class PortalPersonnelTimesheetDto
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime? ClockInAt { get; set; }
    public DateTime? ClockOutAt { get; set; }
    public int BreakMinutes { get; set; }
    public decimal WorkedHours { get; set; }
    public string? Notes { get; set; }
}

public class PortalPersonnelTimesheetUpsertDto
{
    public int PersonnelId { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? ClockInAt { get; set; }
    public DateTime? ClockOutAt { get; set; }
    public int BreakMinutes { get; set; }
    public string? Notes { get; set; }
}

public class PortalAdvanceDto
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime AdvanceDate { get; set; }
    public string? Notes { get; set; }
}

public class PortalAdvanceCreateDto
{
    public int PersonnelId { get; set; }
    public decimal Amount { get; set; }
    public DateTime AdvanceDate { get; set; }
    public string? Notes { get; set; }
}

public class PortalPayrollDto
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal ServiceCommission { get; set; }
    public decimal ProductCommission { get; set; }
    public decimal TotalAdvance { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetPay { get; set; }
    public string? Notes { get; set; }
    public bool IsFinalized { get; set; }
}

public class PortalPayrollGenerateDto
{
    public int PersonnelId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal Deductions { get; set; }
    public string? Notes { get; set; }
    public bool IsFinalized { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// DASHBOARD DTO'LARI
// ═══════════════════════════════════════════════════════════════

public class PortalDashboardDto
{
    public int PersonnelCount { get; set; }
    public int ActiveModuleCount { get; set; }
    public int MaxUsers { get; set; }
    public int CallableUserCount { get; set; }
    public int TotalCallsToday { get; set; }
    public int SipAccountCount { get; set; }
    public List<PortalModuleSummaryDto> Modules { get; set; } = new();
}

public class PortalModuleSummaryDto
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// SIP DTO'LARI
// ═══════════════════════════════════════════════════════════════

public class PortalSipAccountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Transport { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int LineCount { get; set; }
    public int ActiveLineCount { get; set; }
    public List<PortalSipLineDto> Lines { get; set; } = new();
}

public class PortalSipLineDto
{
    public int Id { get; set; }
    public int ChannelNumber { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class PortalSipCreateDto
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 5060;
    [MaxLength(10)] public string? Transport { get; set; } = "UDP";
    public bool IsDefault { get; set; }
    public List<PortalSipLineCreateDto>? Lines { get; set; }
}

public class PortalSipLineCreateDto
{
    public int ChannelNumber { get; set; } = 1;
    [Required, MaxLength(100)] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    [MaxLength(200)] public string? Description { get; set; }
}

public class PortalSipUpdateDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    public bool? IsDefault { get; set; }
}

public class PortalSipLineUpdateDto
{
    public int ChannelNumber { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    public string? Password { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

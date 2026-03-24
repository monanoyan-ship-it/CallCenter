using System.ComponentModel.DataAnnotations;

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
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
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

    public bool IsActive { get; set; } = true;
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

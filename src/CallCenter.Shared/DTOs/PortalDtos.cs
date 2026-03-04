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
    public int PermissionCount { get; set; }
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

public class SetPersonnelPermissionsRequest
{
    public int[] PermissionTypeIds { get; set; } = Array.Empty<int>();
    public int ScopeId { get; set; } = 3; // default: Customer
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
    public string Username { get; set; } = string.Empty;
    public string? Transport { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    // Password portal'da GOSTERILMEZ
}

public class PortalSipUpdateDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool? IsDefault { get; set; }
}

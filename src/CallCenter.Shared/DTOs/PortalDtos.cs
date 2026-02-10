using System.ComponentModel.DataAnnotations;

namespace CallCenter.Shared.DTOs;

// ═══════════════════════════════════════════════════════════════
// USERTYPE DTO'LARI
// ═══════════════════════════════════════════════════════════════

public class UserTypeListDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Level { get; set; }
    public bool CanManageSubordinates { get; set; }
    public bool IsActive { get; set; }
    public int PersonnelCount { get; set; }
    public int PermissionCount { get; set; }
}

public class UserTypeCreateDto
{
    [Required(ErrorMessage = "Tip adi zorunludur.")]
    [MaxLength(100, ErrorMessage = "Tip adi en fazla 100 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int Level { get; set; } = 99;
    public bool CanManageSubordinates { get; set; }
}

public class UserTypeUpdateDto
{
    [Required(ErrorMessage = "Tip adi zorunludur.")]
    [MaxLength(100, ErrorMessage = "Tip adi en fazla 100 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int Level { get; set; } = 99;
    public bool CanManageSubordinates { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UserTypeDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int[] PermissionTypeIds { get; set; } = Array.Empty<int>();
}

public class SetUserTypePermissionsRequest
{
    public int[] PermissionTypeIds { get; set; } = Array.Empty<int>();
}

// ═══════════════════════════════════════════════════════════════
// PORTAL PERSONNEL DTO'LARI
// ═══════════════════════════════════════════════════════════════

public class PortalPersonnelListDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int? UserTypeId { get; set; }
    public string? UserTypeName { get; set; }
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
    [MinLength(6, ErrorMessage = "Sifre en az 6 karakter olmalidir.")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    public int? UserTypeId { get; set; }
    public int? OrganizationUnitId { get; set; }
    public int? ReportsToPersonnelId { get; set; }
}

public class PortalPersonnelUpdateDto
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MinLength(6, ErrorMessage = "Sifre en az 6 karakter olmalidir.")]
    public string? Password { get; set; }

    [MaxLength(100)]
    public string? Title { get; set; }

    public int? UserTypeId { get; set; }
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
    public int UserTypeCount { get; set; }
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

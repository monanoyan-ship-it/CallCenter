namespace CallCenter.Shared.DTOs;

/// <summary>Yetki tipi bilgisi (UI'da checkbox listesi icin)</summary>
public class PermissionTypeDto
{
    public int Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int ModuleId { get; set; }
    public string? ModuleName { get; set; }
}

/// <summary>Portal modulu bilgisi</summary>
public class PortalModuleDto
{
    public int Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public List<PermissionTypeDto> Permissions { get; set; } = new();
}

/// <summary>Personelin mevcut yetkisi</summary>
public class PersonnelPermissionDto
{
    public int Id { get; set; }
    public int PermissionTypeId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string? PermissionDescription { get; set; }
    public string? PermissionIcon { get; set; }
    public int ScopeId { get; set; }
    public string? ScopeName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Description { get; set; }
    public int ModuleId { get; set; }
    public string? ModuleName { get; set; }
}

/// <summary>Toplu yetki atama istegi</summary>
public class AssignPermissionsRequest
{
    public int[] PermissionTypeIds { get; set; } = Array.Empty<int>();
    public int ScopeId { get; set; } = 3; // default: Customer
}

/// <summary>Yetki guncelleme istegi</summary>
public class UpdatePermissionRequest
{
    public int? ScopeId { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Description { get; set; }
}

/// <summary>Musteriye modul atama istegi</summary>
public class AssignModulesRequest
{
    public int[] ModuleIds { get; set; } = Array.Empty<int>();
    public string? Notes { get; set; }
}

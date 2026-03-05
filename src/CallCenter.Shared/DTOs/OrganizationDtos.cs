using System.ComponentModel.DataAnnotations;

namespace CallCenter.Shared.DTOs;

// ═══════════════════════════════════════════════════════════════
// ORGANİZASYON BİRİMİ DTO'LARI
// ═══════════════════════════════════════════════════════════════

public class OrgUnitListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int TypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? TypeIcon { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int ChildCount { get; set; }
    public int PersonnelCount { get; set; }
    public bool IsActive { get; set; }
}

public class OrgUnitTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int TypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? TypeIcon { get; set; }
    public string? TypeCssClass { get; set; }
    public int? ParentId { get; set; }
    public int PersonnelCount { get; set; }
    public int QueueCount { get; set; }
    public int SipAccountCount { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public List<OrgUnitTreeDto> Children { get; set; } = new();
}

public class OrgUnitDetailDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int TypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? TypeIcon { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrgUnitPersonnelDto> Personnel { get; set; } = new();
    public List<OrgUnitChildDto> Children { get; set; } = new();
}

public class OrgUnitPersonnelDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CustomerRoleName { get; set; }
    public int? ReportsToPersonnelId { get; set; }
    public string? ReportsToName { get; set; }
    public bool IsPrimaryAssignment { get; set; } // true = OrganizationUnitId FK, false = junction
}

public class OrgUnitChildDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public int PersonnelCount { get; set; }
}

public class OrgUnitCreateDto
{
    [Required(ErrorMessage = "Birim adi zorunludur.")]
    [MaxLength(200, ErrorMessage = "Birim adi en fazla 200 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Birim tipi zorunludur.")]
    public int TypeId { get; set; }

    public int? ParentId { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    [MaxLength(150)]
    public string? Email { get; set; }

    public int DisplayOrder { get; set; }
}

public class OrgUnitUpdateDto
{
    [Required(ErrorMessage = "Birim adi zorunludur.")]
    [MaxLength(200, ErrorMessage = "Birim adi en fazla 200 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Birim tipi zorunludur.")]
    public int TypeId { get; set; }

    public int? ParentId { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    [MaxLength(150)]
    public string? Email { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Amir ataması DTO</summary>
public class SetReportsToDto
{
    public int? ReportsToPersonnelId { get; set; }
}

/// <summary>Organizasyona personel atama DTO</summary>
public class OrgPersonnelAssignDto
{
    [Required]
    public int PersonnelId { get; set; }
}

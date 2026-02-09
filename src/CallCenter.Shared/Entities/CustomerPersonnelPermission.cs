using CallCenter.Shared.Enums;

namespace CallCenter.Shared.Entities;

/// <summary>
/// Musteri personelinin dinamik yetkileri.
/// Her personele ayri ayri yetki atanabilir.
/// Yetkiler TypeItem olarak tanimli (CustomerPermissionTypes), atamalar DB'de.
/// </summary>
public class CustomerPersonnelPermission
{
    public int Id { get; set; }

    /// <summary>Hangi personele ait</summary>
    public int PersonnelId { get; set; }
    public CustomerPersonnel Personnel { get; set; } = null!;

    /// <summary>Yetki tipi (CustomerPermissionTypes.Ids.*)</summary>
    public int PermissionTypeId { get; set; }

    /// <summary>Yetki kapsami (PermissionScopes.Ids.*, varsayilan: Customer)</summary>
    public int ScopeId { get; set; } = PermissionScopes.Ids.Customer;

    /// <summary>Yetki aktif mi?</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gecerlilik baslangic tarihi (null = hemen gecerli)</summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>Gecerlilik bitis tarihi (null = suresiz)</summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>Aciklama / not</summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Yetkiyi kim atadi</summary>
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}

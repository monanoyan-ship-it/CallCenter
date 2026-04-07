namespace CallCenter.Shared.Entities;

/// <summary>
/// Salon rol-sayfa erişim izni. Merkezi yapı — tüm salonlar için geçerli.
/// Management panelinden düzenlenir, firma bazlı değil.
/// </summary>
public class SalonRolePermission
{
    public int Id { get; set; }

    /// <summary>Rol ID (SalonRoles.Ids.*)</summary>
    public int RoleId { get; set; }

    /// <summary>Sayfa/controller adı (ör: "Clients", "Appointments")</summary>
    public string PageName { get; set; } = string.Empty;

    /// <summary>Bu role bu sayfaya erişim izni var mı?</summary>
    public bool IsAllowed { get; set; } = true;
}

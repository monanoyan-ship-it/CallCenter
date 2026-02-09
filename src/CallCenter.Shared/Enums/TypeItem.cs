namespace CallCenter.Shared.Enums;

/// <summary>
/// Code-based tip tanimlari icin base class.
/// Database yerine kod icerisinde tanimlanan tipler icin kullanilir.
/// Standart enum yerine zengin metadata (icon, css, aciklama) tasir.
/// </summary>
public class TypeItem
{
    public int Id { get; }
    public string SystemName { get; }
    public string NameResourceKey { get; }
    public string? Description { get; }
    public string? Icon { get; }
    public string? CssClass { get; }
    public int DisplayOrder { get; }
    public bool IsDefault { get; }
    public bool IsActive { get; }
    public bool IsSystem { get; }

    public TypeItem(
        int id,
        string systemName,
        string nameResourceKey,
        string? description = null,
        string? icon = null,
        string? cssClass = null,
        int displayOrder = 0,
        bool isDefault = false,
        bool isActive = true,
        bool isSystem = true)
    {
        Id = id;
        SystemName = systemName;
        NameResourceKey = nameResourceKey;
        Description = description;
        Icon = icon;
        CssClass = cssClass;
        DisplayOrder = displayOrder;
        IsDefault = isDefault;
        IsActive = isActive;
        IsSystem = isSystem;
    }
}

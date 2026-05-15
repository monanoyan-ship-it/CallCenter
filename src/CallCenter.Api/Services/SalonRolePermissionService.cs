using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

/// <summary>
/// Salon rol-sayfa izinleri servisi.
/// DB'de kayit varsa DB'den okur, yoksa static SalonRolePermissions fallback kullanir.
/// Management panelinden duzenlenir, tum salonlar icin gecerli.
/// </summary>
public class SalonRolePermissionService
{
    private readonly AppDbContext _db;

    public SalonRolePermissionService(AppDbContext db) => _db = db;

    /// <summary>DB'deki tum izinleri dondurur</summary>
    public async Task<List<SalonRolePermission>> GetAllAsync()
        => await _db.SalonRolePermissions.ToListAsync();

    /// <summary>Belirtilen rolun erisebilecegi sayfalari dondurur (DB + fallback)</summary>
    public async Task<HashSet<string>> GetAccessiblePagesAsync(int roleId)
    {
        var dbPerms = await _db.SalonRolePermissions
            .Where(p => p.RoleId == roleId)
            .ToListAsync();

        // DB'de bu rol icin kayit varsa DB'den don
        if (dbPerms.Count > 0)
        {
            return dbPerms
                .Where(p => p.IsAllowed)
                .Select(p => p.PageName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        // DB'de kayit yoksa static fallback
        return SalonRolePermissions.GetAccessiblePages(roleId);
    }

    /// <summary>Rol-sayfa matrisini toplu kaydet (mevcut kayitlari sil + yeniden olustur)</summary>
    public async Task SaveMatrixAsync(List<SalonRolePermission> permissions)
    {
        // Mevcut tum kayitlari sil
        var existing = await _db.SalonRolePermissions.ToListAsync();
        _db.SalonRolePermissions.RemoveRange(existing);

        // Yeni kayitlari ekle
        _db.SalonRolePermissions.AddRange(permissions);
        await _db.SaveChangesAsync();
    }

    /// <summary>Mevcut static yapiyi DB'ye seed eder (ilk kullanim icin)</summary>
    public async Task<int> SeedFromStaticAsync()
    {
        var existing = await _db.SalonRolePermissions.AnyAsync();
        if (existing) return 0;

        var perms = new List<SalonRolePermission>();
        foreach (var role in SalonRoles.All)
        {
            var pages = SalonRolePermissions.GetAccessiblePages(role.Id);
            foreach (var page in pages)
            {
                perms.Add(new SalonRolePermission
                {
                    RoleId = role.Id,
                    PageName = page,
                    IsAllowed = true
                });
            }
        }

        _db.SalonRolePermissions.AddRange(perms);
        await _db.SaveChangesAsync();
        return perms.Count;
    }

    /// <summary>Tam matris: tum roller x tum sayfalar</summary>
    public async Task<List<RolePageMatrixDto>> GetMatrixAsync()
    {
        var dbPerms = await _db.SalonRolePermissions.ToListAsync();
        var allPages = GetAllPages();
        var result = new List<RolePageMatrixDto>();

        foreach (var role in SalonRoles.All)
        {
            var rolePerms = dbPerms.Where(p => p.RoleId == role.Id).ToList();
            HashSet<string> allowed;

            if (rolePerms.Count > 0)
                allowed = rolePerms.Where(p => p.IsAllowed).Select(p => p.PageName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            else
                allowed = SalonRolePermissions.GetAccessiblePages(role.Id);

            result.Add(new RolePageMatrixDto
            {
                RoleId = role.Id,
                RoleName = role.Description ?? role.SystemName,
                RoleIcon = role.Icon,
                Pages = allPages.Select(page => new PagePermissionDto
                {
                    PageName = page,
                    ModuleId = SalonModuleControllerMap.GetModuleId(page),
                    IsAllowed = allowed.Contains(page)
                }).ToList()
            });
        }

        return result;
    }

    private static List<string> GetAllPages()
    {
        // Tum salon sayfalari (SalonModuleControllerMap'ten + Modules sayfasi)
        var pages = new List<string>
        {
            "Home", "Sales", "Clients", "Appointments", "Waitlist",
            "Services", "Staff", "Recipes", "PersonnelPrices",
            "Products", "Suppliers",
            "Invoices", "Cash", "Expenses", "GiftCards", "Packages",
            "Campaigns", "Memberships", "Loyalty", "EmailCampaigns", "Reviews", "Winback",
            "Reports",
            "Profile", "Branches", "NoShowPolicy", "ConsentForms", "BeforeAfter", "EmailSettings", "PageSettings", "PaymentInfo",
            "Modules"
        };
        return pages;
    }
}

public class RolePageMatrixDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? RoleIcon { get; set; }
    public List<PagePermissionDto> Pages { get; set; } = new();
}

public class PagePermissionDto
{
    public string PageName { get; set; } = string.Empty;
    public int? ModuleId { get; set; }
    public bool IsAllowed { get; set; }
}

public class RolePermissionSaveItem
{
    public int RoleId { get; set; }
    public string PageName { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
}

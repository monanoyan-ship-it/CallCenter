using System.Security.Claims;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Components.Authorization;

namespace CallCenter.Web.Services;

/// <summary>
/// JWT claim'lerinden CustomerUser yetkilerini parse eder.
/// NavMenu ve portal sayfalari icin client-side yetki kontrolu.
/// </summary>
public class PermissionService
{
    private readonly AuthenticationStateProvider _authState;
    private HashSet<int> _permissions = new();
    private HashSet<int> _modules = new();
    private bool _loaded;

    public bool IsAdmin { get; private set; }
    public bool IsCustomerAdmin { get; private set; }
    public int? CustomerId { get; private set; }
    public int CustomerRoleId { get; private set; }
    public int? CustomerPersonnelId { get; private set; }
    public bool IsFirmaAdmin => IsCustomerAdmin || CustomerRoleId == CustomerRoles.Ids.FirmaAdmin;
    public bool IsEkipLideri => CustomerRoleId == CustomerRoles.Ids.EkipLideri;

    public PermissionService(AuthenticationStateProvider authState)
    {
        _authState = authState;
    }

    public void Reset()
    {
        _loaded = false;
        _permissions = new();
        _modules = new();
        IsAdmin = false;
        IsCustomerAdmin = false;
        CustomerId = null;
        CustomerRoleId = 0;
        CustomerPersonnelId = null;
    }

    public async Task LoadAsync()
    {
        if (_loaded) return;

        var state = await _authState.GetAuthenticationStateAsync();
        var user = state.User;

        if (user.Identity?.IsAuthenticated != true) return;

        IsAdmin = user.IsInRole("Admin");
        IsCustomerAdmin = user.FindFirst("IsCustomerAdmin")?.Value == "true";

        var customerIdClaim = user.FindFirst("CustomerId")?.Value;
        CustomerId = customerIdClaim != null && int.TryParse(customerIdClaim, out var cid) ? cid : null;

        var roleClaim = user.FindFirst("CustomerRoleId")?.Value;
        CustomerRoleId = roleClaim != null && int.TryParse(roleClaim, out var rid) ? rid : 0;

        var personnelIdClaim = user.FindFirst("CustomerPersonnelId")?.Value;
        CustomerPersonnelId = personnelIdClaim != null && int.TryParse(personnelIdClaim, out var pid) ? pid : null;

        var permsClaim = user.FindFirst("CustomerPermissions")?.Value;
        if (!string.IsNullOrEmpty(permsClaim))
        {
            _permissions = permsClaim
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => int.TryParse(p.Trim(), out _))
                .Select(p => int.Parse(p.Trim()))
                .ToHashSet();
        }

        var modulesClaim = user.FindFirst("CustomerModules")?.Value;
        if (!string.IsNullOrEmpty(modulesClaim))
        {
            _modules = modulesClaim
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Where(m => int.TryParse(m.Trim(), out _))
                .Select(m => int.Parse(m.Trim()))
                .ToHashSet();
        }

        _loaded = true;
    }

    /// <summary>Tekil izin kontrolu. System Admin ve CustomerAdmin her zaman true doner.</summary>
    public bool HasPermission(int permTypeId)
    {
        if (IsAdmin || IsCustomerAdmin) return true;
        return _permissions.Contains(permTypeId);
    }

    /// <summary>Musteri bu modulu satin almis mi? Admin her zaman true.</summary>
    public bool HasModule(int moduleId)
    {
        if (IsAdmin) return true;
        return _modules.Contains(moduleId);
    }
}

using System.Security.Claims;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Components.Authorization;

namespace CallCenter.Windows.Services;

/// <summary>
/// JWT claim'lerinden CustomerUser yetkilerini parse eder.
/// NavMenu ve portal sayfalari icin client-side yetki kontrolu.
/// </summary>
public class WindowsPermissionService
{
    private readonly AuthenticationStateProvider _authState;
    private HashSet<int> _permissions = new();

    public bool IsAdmin { get; private set; }
    public int? CustomerId { get; private set; }
    public int CustomerRoleId { get; private set; }

    public WindowsPermissionService(AuthenticationStateProvider authState)
    {
        _authState = authState;
    }

    public void Reset()
    {
        _permissions = new();
        IsAdmin = false;
        CustomerId = null;
        CustomerRoleId = 0;
    }

    public async Task LoadAsync()
    {
        Reset();

        var state = await _authState.GetAuthenticationStateAsync();
        var user = state.User;

        if (user.Identity?.IsAuthenticated != true) return;

        IsAdmin = user.IsInRole("Admin");

        var customerIdClaim = user.FindFirst("CustomerId")?.Value;
        CustomerId = customerIdClaim != null && int.TryParse(customerIdClaim, out var cid) ? cid : null;

        var roleClaim = user.FindFirst("CustomerRoleId")?.Value;
        CustomerRoleId = roleClaim != null && int.TryParse(roleClaim, out var rid) ? rid : 0;

        var permsClaim = user.FindFirst("CustomerPermissions")?.Value;
        if (!string.IsNullOrEmpty(permsClaim))
        {
            _permissions = permsClaim
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => int.TryParse(p.Trim(), out _))
                .Select(p => int.Parse(p.Trim()))
                .ToHashSet();
        }
    }

    /// <summary>Tekil izin kontrolu. Admin her zaman true doner.</summary>
    public bool HasPermission(int permTypeId)
    {
        if (IsAdmin) return true;
        return _permissions.Contains(permTypeId);
    }

    /// <summary>Tum moduller her zaman aktif (dinamik modul yonetimi kaldirildi).</summary>
    public bool HasModule(int moduleId) => true;
}

using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

/// <summary>
/// Audit log kolaylik metotlari sunan base controller.
/// HttpContext'ten userId, userName, IP, UserAgent otomatik alinir.
/// </summary>
public abstract class AuditableControllerBase : ControllerBase
{
    private readonly IAuditFactory _auditFactory;

    protected AuditableControllerBase(IAuditFactory auditFactory)
    {
        _auditFactory = auditFactory;
    }

    // ───────── Yardimci property'ler ─────────

    protected int? CurrentUserId
    {
        get
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim) : null;
        }
    }

    protected string? CurrentUserName => User.FindFirstValue(ClaimTypes.Name)
        ?? User.FindFirstValue("FullName")
        ?? User.Identity?.Name;

    protected string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    protected string? ClientUserAgent => Request.Headers.UserAgent.ToString();

    protected int? CurrentCustomerId
    {
        get
        {
            var claim = User.FindFirstValue("CustomerId");
            return claim != null && int.TryParse(claim, out var id) ? id : null;
        }
    }

    // ───────── Audit log kolaylik metotlari ─────────

    /// <summary>Auth olaylarini loglar (login, logout, refresh, revoke)</summary>
    protected async Task AuditAuthAsync(string action, string description, int? userId = null, string? userName = null)
    {
        await _auditFactory.LogAuthAsync(action, description,
            userId ?? CurrentUserId,
            userName ?? CurrentUserName,
            ClientIp, ClientUserAgent);
    }

    /// <summary>CRUD olaylarini loglar</summary>
    protected async Task AuditCrudAsync(string action, string entityType, string? entityId, string description,
        string? oldValues = null, string? newValues = null, int? customerId = null)
    {
        await _auditFactory.LogCrudAsync(action, entityType, entityId, description,
            CurrentUserId, CurrentUserName,
            oldValues, newValues,
            customerId ?? CurrentCustomerId,
            ClientIp, ClientUserAgent);
    }
}

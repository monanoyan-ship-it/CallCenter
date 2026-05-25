using System.Security.Claims;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Api.Filters;

/// <summary>
/// Salon API action'larini mevcut SalonRolePermissions sayfa matrisiyle sinirlar.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSalonPageAttribute : ActionFilterAttribute
{
    private readonly string _pageName;
    public string PageName => _pageName;

    public RequireSalonPageAttribute(string pageName)
    {
        _pageName = pageName;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Filters.Any(f => f is IAllowAnonymousFilter))
        {
            base.OnActionExecuting(context);
            return;
        }

        var user = context.HttpContext.User;
        if (user.IsInRole("Admin"))
        {
            base.OnActionExecuting(context);
            return;
        }

        var roleClaim = user.FindFirstValue("CustomerRoleId");
        if (int.TryParse(roleClaim, out var roleId) && SalonRolePermissions.CanAccess(roleId, _pageName))
        {
            base.OnActionExecuting(context);
            return;
        }

        context.Result = new ObjectResult(new { message = "Bu islem icin yetkiniz yok." })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}

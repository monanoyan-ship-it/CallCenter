using System.Security.Claims;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Api.Filters;

/// <summary>
/// Salon sahibi disindaki salon rollerinin firma/public ayarlarini degistirmesini engeller.
/// Admin ve AllowAnonymous aksiyonlar muaf tutulur.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSalonOwnerAttribute : ActionFilterAttribute
{
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
        if (int.TryParse(roleClaim, out var roleId) && roleId == SalonRoles.Ids.SalonOwner)
        {
            base.OnActionExecuting(context);
            return;
        }

        context.Result = new ObjectResult(new { message = "Bu islem sadece salon sahibi tarafindan yapilabilir." })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}

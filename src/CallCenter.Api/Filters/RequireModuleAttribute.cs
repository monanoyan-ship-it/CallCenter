using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Api.Filters;

/// <summary>
/// API controller/action uzerinde modul bazli erisim kontrolu yapar.
/// Token'daki CustomerModules claim'inden aktif modul ID'leri kontrol edilir.
/// Modul aktif degilse 403 doner.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireModuleAttribute : ActionFilterAttribute
{
    private readonly int _moduleId;

    public RequireModuleAttribute(int moduleId)
    {
        _moduleId = moduleId;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var modulesClaim = context.HttpContext.User.FindFirstValue("CustomerModules");

        // CustomerModules claim yoksa (admin kullanici vb.) gecis izni ver
        if (string.IsNullOrEmpty(modulesClaim))
        {
            base.OnActionExecuting(context);
            return;
        }

        var moduleIds = modulesClaim.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

        if (!moduleIds.Contains(_moduleId))
        {
            context.Result = new ObjectResult(new { message = "Bu module erisim yetkiniz yok." })
            {
                StatusCode = 403
            };
            return;
        }

        base.OnActionExecuting(context);
    }
}

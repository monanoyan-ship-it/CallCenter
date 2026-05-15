using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Api.Filters;

/// <summary>
/// Token'daki CustomerModules claim'inde verilen modullerden en az biri varsa gecis izni verir.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAnyModuleAttribute : ActionFilterAttribute
{
    private readonly HashSet<int> _moduleIds;

    public RequireAnyModuleAttribute(params int[] moduleIds)
    {
        _moduleIds = moduleIds.Where(id => id > 0).ToHashSet();
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var modulesClaim = context.HttpContext.User.FindFirstValue("CustomerModules");

        if (string.IsNullOrEmpty(modulesClaim))
        {
            base.OnActionExecuting(context);
            return;
        }

        var activeModuleIds = modulesClaim.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

        if (!_moduleIds.Any(activeModuleIds.Contains))
        {
            context.Result = new ObjectResult(new { message = "Bu modüle erişim yetkiniz yok." })
            {
                StatusCode = 403
            };
            return;
        }

        base.OnActionExecuting(context);
    }
}

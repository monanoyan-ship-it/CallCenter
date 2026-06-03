using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-dashboard")]
[Authorize]
public class SlnDashboardController : ControllerBase
{
    private readonly ISlnDashboardFactory _factory;

    public SlnDashboardController(ISlnDashboardFactory factory) => _factory = factory;

    [HttpGet]
    public async Task<ActionResult> GetDashboard()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetDashboardAsync(customerId, GetBranchId(), GetRoleId(), GetPersonnelId()));
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    private int GetRoleId()
        => int.TryParse(User.FindFirst("CustomerRoleId")?.Value, out var id) ? id : 0;

    private int GetPersonnelId()
        => int.TryParse(User.FindFirst("CustomerPersonnelId")?.Value, out var id) ? id : 0;
}

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

    // Hizmet personeli kendi hakedisi (ciro + prim). Default donem: bu ayin 1'i -> bugun (UTC).
    [HttpGet("my-earnings")]
    public async Task<ActionResult> GetMyEarnings([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (startUtc, endUtc) = ResolveRange(startDate, endDate);
        return Ok(await _factory.GetMyEarningsAsync(customerId, GetPersonnelId(), startUtc, endUtc, GetBranchId()));
    }

    private static (DateTime StartUtc, DateTime EndUtc) ResolveRange(DateTime? startDate, DateTime? endDate)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var start = startDate?.Date ?? new DateTime(todayUtc.Year, todayUtc.Month, 1);
        var endInclusive = endDate?.Date ?? todayUtc;
        var startUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        // Bitis tarihi dahil olsun diye +1 gun (exclusive ust sinir)
        var endUtc = DateTime.SpecifyKind(endInclusive, DateTimeKind.Utc).AddDays(1);
        return (startUtc, endUtc);
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

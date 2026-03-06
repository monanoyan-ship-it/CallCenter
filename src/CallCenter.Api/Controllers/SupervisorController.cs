using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor,CustomerUser")]
public class SupervisorController : ControllerBase
{
    private readonly ISupervisorFactory _supervisorFactory;

    public SupervisorController(ISupervisorFactory supervisorFactory)
    {
        _supervisorFactory = supervisorFactory;
    }

    private bool IsSystemAdmin => User.IsInRole("Admin") || User.IsInRole("Supervisor");

    private int? ResolveCustomerId(int? queryCustomerId)
    {
        if (IsSystemAdmin)
            return queryCustomerId;
        var claim = User.FindFirstValue("CustomerId");
        return claim != null ? int.Parse(claim) : null;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard([FromQuery] int? customerId)
    {
        var result = await _supervisorFactory.GetDashboardAsync(ResolveCustomerId(customerId));

        // Sistem adminleri bireysel arama kayitlarini goremez — sadece KPI sayilari yeter
        if (IsSystemAdmin)
        {
            result.RecentCalls = new();
        }

        return Ok(result);
    }

    [HttpGet("queues/live")]
    public async Task<ActionResult<List<QueueLiveDto>>> GetQueuesLive([FromQuery] int? customerId)
    {
        return Ok(await _supervisorFactory.GetQueuesLiveAsync(ResolveCustomerId(customerId)));
    }

    [HttpGet("customers")]
    public async Task<ActionResult<List<CustomerSimpleDto>>> GetCustomers()
    {
        // CustomerUser kendi firmasini zaten biliyor, tum listeyi gormesine gerek yok
        // Ama 403 vermek yerine kendi firmasini donelim
        if (User.IsInRole("CustomerUser"))
        {
            var claim = User.FindFirstValue("CustomerId");
            if (claim != null && int.TryParse(claim, out var cid))
            {
                var all = await _supervisorFactory.GetCustomersAsync();
                return Ok(all.Where(c => c.Id == cid).ToList());
            }
            return Ok(new List<CustomerSimpleDto>());
        }

        return Ok(await _supervisorFactory.GetCustomersAsync());
    }
}

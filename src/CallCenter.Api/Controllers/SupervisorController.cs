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

    private int? ResolveCustomerId(int? queryCustomerId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Supervisor"))
            return queryCustomerId;
        var claim = User.FindFirstValue("CustomerId");
        return claim != null ? int.Parse(claim) : null;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard([FromQuery] int? customerId)
    {
        return Ok(await _supervisorFactory.GetDashboardAsync(ResolveCustomerId(customerId)));
    }

    [HttpGet("queues/live")]
    public async Task<ActionResult<List<QueueLiveDto>>> GetQueuesLive([FromQuery] int? customerId)
    {
        return Ok(await _supervisorFactory.GetQueuesLiveAsync(ResolveCustomerId(customerId)));
    }

    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet("customers")]
    public async Task<ActionResult<List<CustomerSimpleDto>>> GetCustomers()
    {
        return Ok(await _supervisorFactory.GetCustomersAsync());
    }
}

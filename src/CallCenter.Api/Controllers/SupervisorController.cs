using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class SupervisorController : ControllerBase
{
    private readonly ISupervisorFactory _supervisorFactory;

    public SupervisorController(ISupervisorFactory supervisorFactory)
    {
        _supervisorFactory = supervisorFactory;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard([FromQuery] int? customerId)
    {
        return Ok(await _supervisorFactory.GetDashboardAsync(customerId));
    }

    [HttpGet("queues/live")]
    public async Task<ActionResult<List<QueueLiveDto>>> GetQueuesLive([FromQuery] int? customerId)
    {
        return Ok(await _supervisorFactory.GetQueuesLiveAsync(customerId));
    }

    [HttpGet("customers")]
    public async Task<ActionResult<List<CustomerSimpleDto>>> GetCustomers()
    {
        return Ok(await _supervisorFactory.GetCustomersAsync());
    }
}

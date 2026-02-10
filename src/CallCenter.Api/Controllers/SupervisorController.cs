using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class SupervisorController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public SupervisorController(ServiceFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Supervisor dashboard verilerini tek seferde dondurur.
    /// customerId verilirse sadece o firmanin verileri, verilmezse tum sistem.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard([FromQuery] int? customerId)
    {
        var svc = _factory.CreateSupervisorService();
        return Ok(await svc.GetDashboardAsync(customerId));
    }

    /// <summary>
    /// Canli kuyruk izleme — her kuyrugun anlik durumu + agent listesi.
    /// </summary>
    [HttpGet("queues/live")]
    public async Task<ActionResult<List<QueueLiveDto>>> GetQueuesLive([FromQuery] int? customerId)
    {
        var svc = _factory.CreateSupervisorService();
        return Ok(await svc.GetQueuesLiveAsync(customerId));
    }

    /// <summary>
    /// Dashboard'daki firma dropdown icin musteri listesi.
    /// </summary>
    [HttpGet("customers")]
    public async Task<ActionResult<List<CustomerSimpleDto>>> GetCustomers()
    {
        var svc = _factory.CreateSupervisorService();
        return Ok(await svc.GetCustomersAsync());
    }
}

using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CrmController : ControllerBase
{
    private readonly ICrmFactory _crm;

    public CrmController(ICrmFactory crm) => _crm = crm;

    // ─── Dashboard ───

    [HttpGet("dashboard")]
    public async Task<ActionResult<CrmDashboardDto>> GetDashboard()
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        return Ok(await _crm.GetDashboardAsync(cid.Value));
    }

    // ─── Caller ID Lookup ───

    [HttpGet("contacts/lookup")]
    public async Task<ActionResult<CrmCallerIdDto>> LookupContact([FromQuery] string phone)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();
        if (string.IsNullOrWhiteSpace(phone)) return BadRequest(new { error = "phone parametresi gerekli" });

        var result = await _crm.LookupByPhoneAsync(cid.Value, phone);
        return result != null ? Ok(result) : NotFound();
    }

    // ─── Contacts ───

    [HttpGet("contacts")]
    public async Task<ActionResult<List<CrmContactDto>>> GetContacts([FromQuery] string? search)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        return Ok(await _crm.GetContactsAsync(cid.Value, search));
    }

    [HttpGet("contacts/{id}")]
    public async Task<ActionResult<CrmContactDto>> GetContactDetail(int id)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        var result = await _crm.GetContactDetailAsync(id, cid.Value);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpPost("contacts")]
    public async Task<ActionResult> CreateContact([FromBody] CrmContactCreateDto dto)
    {
        var (cid, pid) = GetIds();
        if (cid == null || pid == null) return Forbid();

        var (success, id, error) = await _crm.CreateContactAsync(dto, cid.Value, pid.Value);
        return success ? Ok(new { id }) : BadRequest(new { error });
    }

    [HttpPut("contacts/{id}")]
    public async Task<ActionResult> UpdateContact(int id, [FromBody] CrmContactUpdateDto dto)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        var (success, error) = await _crm.UpdateContactAsync(id, dto, cid.Value);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("contacts/{id}")]
    public async Task<ActionResult> DeleteContact(int id)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        var (success, error) = await _crm.DeleteContactAsync(id, cid.Value);
        return success ? Ok() : BadRequest(new { error });
    }

    // ─── Tickets ───

    [HttpGet("tickets")]
    public async Task<ActionResult<List<CrmTicketDto>>> GetTickets([FromQuery] int? statusId)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        return Ok(await _crm.GetTicketsAsync(cid.Value, statusId));
    }

    [HttpGet("tickets/{id}")]
    public async Task<ActionResult<CrmTicketDto>> GetTicketDetail(int id)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        var result = await _crm.GetTicketDetailAsync(id, cid.Value);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpPost("tickets")]
    public async Task<ActionResult> CreateTicket([FromBody] CrmTicketCreateDto dto)
    {
        var (cid, pid) = GetIds();
        if (cid == null || pid == null) return Forbid();

        var (success, id, error) = await _crm.CreateTicketAsync(dto, cid.Value, pid.Value);
        return success ? Ok(new { id }) : BadRequest(new { error });
    }

    [HttpPut("tickets/{id}")]
    public async Task<ActionResult> UpdateTicket(int id, [FromBody] CrmTicketUpdateDto dto)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        var (success, error) = await _crm.UpdateTicketAsync(id, dto, cid.Value);
        return success ? Ok() : BadRequest(new { error });
    }

    // ─── Deals ───

    [HttpGet("deals")]
    public async Task<ActionResult<List<CrmDealDto>>> GetDeals([FromQuery] int? stageId)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        return Ok(await _crm.GetDealsAsync(cid.Value, stageId));
    }

    [HttpGet("deals/{id}")]
    public async Task<ActionResult<CrmDealDto>> GetDealDetail(int id)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        var result = await _crm.GetDealDetailAsync(id, cid.Value);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpPost("deals")]
    public async Task<ActionResult> CreateDeal([FromBody] CrmDealCreateDto dto)
    {
        var (cid, pid) = GetIds();
        if (cid == null || pid == null) return Forbid();

        var (success, id, error) = await _crm.CreateDealAsync(dto, cid.Value, pid.Value);
        return success ? Ok(new { id }) : BadRequest(new { error });
    }

    [HttpPut("deals/{id}")]
    public async Task<ActionResult> UpdateDeal(int id, [FromBody] CrmDealUpdateDto dto)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        var (success, error) = await _crm.UpdateDealAsync(id, dto, cid.Value);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("deals/{id}")]
    public async Task<ActionResult> DeleteDeal(int id)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        var (success, error) = await _crm.DeleteDealAsync(id, cid.Value);
        return success ? Ok() : BadRequest(new { error });
    }

    // ─── Activities ───

    [HttpGet("activities")]
    public async Task<ActionResult<List<CrmActivityDto>>> GetActivities(
        [FromQuery] int? contactId, [FromQuery] int? ticketId, [FromQuery] int? dealId)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        return Ok(await _crm.GetActivitiesAsync(cid.Value, contactId, ticketId, dealId));
    }

    [HttpPost("activities")]
    public async Task<ActionResult> CreateActivity([FromBody] CrmActivityCreateDto dto)
    {
        var (cid, pid) = GetIds();
        if (cid == null || pid == null) return Forbid();

        var (success, id, error) = await _crm.CreateActivityAsync(dto, cid.Value, pid.Value);
        return success ? Ok(new { id }) : BadRequest(new { error });
    }

    // ─── Tasks ───

    [HttpGet("tasks")]
    public async Task<ActionResult<List<CrmTaskDto>>> GetTasks(
        [FromQuery] int? assignedTo, [FromQuery] int? statusId)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        return Ok(await _crm.GetTasksAsync(cid.Value, assignedTo, statusId));
    }

    [HttpPost("tasks")]
    public async Task<ActionResult> CreateTask([FromBody] CrmTaskCreateDto dto)
    {
        var (cid, pid) = GetIds();
        if (cid == null || pid == null) return Forbid();

        var (success, id, error) = await _crm.CreateTaskAsync(dto, cid.Value, pid.Value);
        return success ? Ok(new { id }) : BadRequest(new { error });
    }

    [HttpPut("tasks/{id}")]
    public async Task<ActionResult> UpdateTask(int id, [FromBody] CrmTaskUpdateDto dto)
    {
        var cid = GetCustomerId();
        if (cid == null) return Forbid();

        var (success, error) = await _crm.UpdateTaskAsync(id, dto, cid.Value);
        return success ? Ok() : BadRequest(new { error });
    }

    // ─── Helpers ───

    private int? GetCustomerId()
    {
        var claim = User.FindFirst("CustomerId")?.Value;
        return claim != null ? int.Parse(claim) : null;
    }

    private int? GetPersonnelId()
    {
        var claim = User.FindFirst("CustomerPersonnelId")?.Value;
        return claim != null ? int.Parse(claim) : null;
    }

    private (int? customerId, int? personnelId) GetIds()
        => (GetCustomerId(), GetPersonnelId());
}

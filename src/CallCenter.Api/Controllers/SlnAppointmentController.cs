using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-appointments")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnAppointments)]
public class SlnAppointmentController : ControllerBase
{
    private readonly ISlnAppointmentFactory _appointmentFactory;

    public SlnAppointmentController(ISlnAppointmentFactory appointmentFactory) => _appointmentFactory = appointmentFactory;

    [HttpGet]
    public async Task<ActionResult<List<SlnAppointmentDto>>> GetAppointments(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int? personnelId, [FromQuery] int? statusId,
        [FromQuery] int? slnClientId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var appointments = await _appointmentFactory.GetAppointmentsAsync(customerId, from, to, personnelId, statusId, GetBranchId(), slnClientId);
        return Ok(appointments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnAppointmentDto>> GetAppointment(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var appointment = await _appointmentFactory.GetAppointmentAsync(id, customerId);
        return appointment != null ? Ok(appointment) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnAppointmentDto>> CreateAppointment([FromBody] SlnAppointmentCreateDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (appointment, error) = await _appointmentFactory.CreateAppointmentAsync(dto, userId, customerId, GetBranchId());
        return appointment != null ? Ok(appointment) : BadRequest(error);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAppointment(int id, [FromBody] SlnAppointmentCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _appointmentFactory.UpdateAppointmentAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] SlnAppointmentStatusRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error, penalty) = await _appointmentFactory.UpdateStatusAsync(id, req.StatusId, customerId);
        if (!success) return BadRequest(error);
        return Ok(new { penalty, message = penalty > 0 ? $"{penalty:F0} TL ceza uygulandi" : (string?)null });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAppointment(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _appointmentFactory.DeleteAppointmentAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>Belirli personel ve saat araligi icin cakisma kontrolu</summary>
    [HttpGet("check-conflict")]
    public async Task<ActionResult<bool>> CheckConflict(
        [FromQuery] int personnelId, [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime, [FromQuery] int? excludeId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var hasConflict = await _appointmentFactory.CheckConflictAsync(personnelId, startTime, endTime, customerId, excludeId);
        return Ok(hasConflict);
    }

    /// <summary>Secilen hizmetleri yapabilecek personeller</summary>
    [HttpGet("available-staff")]
    public async Task<ActionResult> GetAvailableStaff([FromQuery] string serviceIds)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var ids = (serviceIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
            .Where(id => id > 0).ToList();

        var staff = await _appointmentFactory.GetAvailableStaffAsync(customerId, ids, GetBranchId());
        return Ok(staff);
    }

    /// <summary>Personelin belirtilen gundeki musait saat slotlari</summary>
    [HttpGet("available-slots")]
    public async Task<ActionResult> GetAvailableSlots([FromQuery] int personnelId, [FromQuery] DateTime date, [FromQuery] int durationMinutes = 30)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var slots = await _appointmentFactory.GetAvailableSlotsAsync(customerId, personnelId, date, durationMinutes, GetBranchId());
        return Ok(slots);
    }

    private int GetUserId()
        => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}

public class SlnAppointmentStatusRequest
{
    public int StatusId { get; set; }
}

using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class QueuesController : AuditableControllerBase
{
    public QueuesController(ServiceFactory factory) : base(factory) { }

    /// <summary>Sayfalamali kuyruk listesi</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<QueueListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? customerId = null,
        [FromQuery] string? search = null)
    {
        var svc = Factory.CreateQueueService();
        return Ok(await svc.GetAllAsync(page, pageSize, customerId, search));
    }

    /// <summary>Kuyruk detay (atanmis agent'lar dahil)</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<QueueDetailDto>> GetById(int id)
    {
        var svc = Factory.CreateQueueService();
        var result = await svc.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Kuyruk bulunamadi." });
        return Ok(result);
    }

    /// <summary>Yeni kuyruk olustur</summary>
    [HttpPost]
    public async Task<ActionResult> Create(QueueCreateDto dto)
    {
        var svc = Factory.CreateQueueService();
        var (success, id, error) = await svc.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "Queue", id.ToString(),
            $"Kuyruk olusturuldu: '{dto.Name}'", customerId: dto.CustomerId);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Kuyruk guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, QueueUpdateDto dto)
    {
        var svc = Factory.CreateQueueService();
        var (success, error) = await svc.UpdateAsync(id, dto);
        if (!success)
        {
            if (error == "Kuyruk bulunamadi.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }

        await AuditCrudAsync("Update", "Queue", id.ToString(),
            $"Kuyruk guncellendi: ID={id}");

        return NoContent();
    }

    /// <summary>Kuyruk sil (soft delete)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var svc = Factory.CreateQueueService();
        var (success, error) = await svc.DeleteAsync(id);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "Queue", id.ToString(),
            $"Kuyruk silindi: ID={id}");

        return NoContent();
    }

    /// <summary>Kuyruga agent ata</summary>
    [HttpPost("{id}/agents")]
    public async Task<ActionResult> AssignAgent(int id, QueueAgentAssignDto dto)
    {
        var svc = Factory.CreateQueueService();
        var (success, error) = await svc.AssignAgentAsync(id, dto);
        if (!success)
        {
            if (error!.Contains("bulunamadi")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }

        await AuditCrudAsync("AssignAgent", "Queue", id.ToString(),
            $"Kuyruk ID={id}'e agent ID={dto.AgentId} atandi.");

        return Ok(new { message = "Temsilci kuyruga atandi." });
    }

    /// <summary>Kuyruktan agent cikar</summary>
    [HttpDelete("{id}/agents/{agentId}")]
    public async Task<ActionResult> RemoveAgent(int id, int agentId)
    {
        var svc = Factory.CreateQueueService();
        var (success, error) = await svc.RemoveAgentAsync(id, agentId);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("RemoveAgent", "Queue", id.ToString(),
            $"Kuyruk ID={id}'den agent ID={agentId} cikarildi.");

        return NoContent();
    }
}

using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class QueuesController : ControllerBase
{
    private readonly AppDbContext _db;

    public QueuesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Sayfalamali kuyruk listesi</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<QueueListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? customerId = null,
        [FromQuery] string? search = null)
    {
        var query = _db.Queues.Include(q => q.Customer).AsQueryable();

        if (customerId.HasValue && customerId.Value > 0)
        {
            query = query.Where(q => q.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(q => q.Name.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(q => q.Customer.Name).ThenBy(q => q.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new QueueListDto
            {
                Id = q.Id,
                Name = q.Name,
                Description = q.Description,
                MaxWaitTimeSeconds = q.MaxWaitTimeSeconds,
                IsActive = q.IsActive,
                CustomerId = q.CustomerId,
                CustomerName = q.Customer.Name,
                AgentCount = q.QueueAgents.Count
            })
            .ToListAsync();

        return Ok(new PagedResult<QueueListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>Kuyruk detay (atanmis agent'lar dahil)</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<QueueDetailDto>> GetById(int id)
    {
        var q = await _db.Queues
            .Include(x => x.Customer)
            .Include(x => x.QueueAgents)
                .ThenInclude(qa => qa.Agent)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (q == null) return NotFound(new { message = "Kuyruk bulunamadi." });

        return Ok(new QueueDetailDto
        {
            Id = q.Id,
            Name = q.Name,
            Description = q.Description,
            MaxWaitTimeSeconds = q.MaxWaitTimeSeconds,
            IsActive = q.IsActive,
            CustomerId = q.CustomerId,
            CustomerName = q.Customer.Name,
            Agents = q.QueueAgents.Select(qa =>
            {
                var status = AgentStatuses.GetById(qa.Agent.StatusId);
                return new QueueAgentDto
                {
                    AgentId = qa.AgentId,
                    FullName = qa.Agent.FullName,
                    Extension = qa.Agent.Extension,
                    StatusId = qa.Agent.StatusId,
                    StatusName = status?.SystemName ?? "Offline"
                };
            }).ToList()
        });
    }

    /// <summary>Yeni kuyruk olustur</summary>
    [HttpPost]
    public async Task<ActionResult> Create(QueueCreateDto dto)
    {
        // Unique kontrol: ayni firma icinde ayni kuyruk adi olamaz
        if (await _db.Queues.AnyAsync(q => q.CustomerId == dto.CustomerId && q.Name == dto.Name))
            return BadRequest(new { message = "Bu firmada ayni isimde kuyruk zaten var." });

        var queue = new Queue
        {
            Name = dto.Name,
            Description = dto.Description,
            MaxWaitTimeSeconds = dto.MaxWaitTimeSeconds,
            CustomerId = dto.CustomerId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Queues.Add(queue);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = queue.Id }, new { id = queue.Id });
    }

    /// <summary>Kuyruk guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, QueueUpdateDto dto)
    {
        var queue = await _db.Queues.FindAsync(id);
        if (queue == null) return NotFound(new { message = "Kuyruk bulunamadi." });

        // Unique kontrol (kendisi haric)
        if (await _db.Queues.AnyAsync(q => q.CustomerId == queue.CustomerId && q.Name == dto.Name && q.Id != id))
            return BadRequest(new { message = "Bu firmada ayni isimde kuyruk zaten var." });

        queue.Name = dto.Name;
        queue.Description = dto.Description;
        queue.MaxWaitTimeSeconds = dto.MaxWaitTimeSeconds;
        queue.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Kuyruk sil (soft delete)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var queue = await _db.Queues.FindAsync(id);
        if (queue == null) return NotFound(new { message = "Kuyruk bulunamadi." });

        queue.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Kuyruga agent ata</summary>
    [HttpPost("{id}/agents")]
    public async Task<ActionResult> AssignAgent(int id, QueueAgentAssignDto dto)
    {
        var queue = await _db.Queues.FindAsync(id);
        if (queue == null) return NotFound(new { message = "Kuyruk bulunamadi." });

        var agent = await _db.Users.FindAsync(dto.AgentId);
        if (agent == null) return NotFound(new { message = "Temsilci bulunamadi." });

        // Zaten atanmis mi?
        if (await _db.QueueAgents.AnyAsync(qa => qa.QueueId == id && qa.AgentId == dto.AgentId))
            return BadRequest(new { message = "Temsilci zaten bu kuyruga atanmis." });

        _db.QueueAgents.Add(new QueueAgent
        {
            QueueId = id,
            AgentId = dto.AgentId
        });
        await _db.SaveChangesAsync();

        return Ok(new { message = "Temsilci kuyruga atandi." });
    }

    /// <summary>Kuyruktan agent cikar</summary>
    [HttpDelete("{id}/agents/{agentId}")]
    public async Task<ActionResult> RemoveAgent(int id, int agentId)
    {
        var qa = await _db.QueueAgents.FirstOrDefaultAsync(x => x.QueueId == id && x.AgentId == agentId);
        if (qa == null) return NotFound(new { message = "Atama bulunamadi." });

        _db.QueueAgents.Remove(qa);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

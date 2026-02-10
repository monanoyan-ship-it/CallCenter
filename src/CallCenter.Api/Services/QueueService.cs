using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class QueueService : IQueueService
{
    private readonly AppDbContext _db;

    public QueueService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<QueueListDto>> GetAllAsync(int page, int pageSize, int? customerId, string? search)
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
                OrganizationUnitId = q.OrganizationUnitId,
                OrganizationUnitName = q.OrganizationUnit != null ? q.OrganizationUnit.Name : null,
                AgentCount = q.QueueAgents.Count
            })
            .ToListAsync();

        return new PagedResult<QueueListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<QueueDetailDto?> GetByIdAsync(int id)
    {
        var q = await _db.Queues
            .Include(x => x.Customer)
            .Include(x => x.QueueAgents)
                .ThenInclude(qa => qa.Agent)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (q == null) return null;

        return new QueueDetailDto
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
        };
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateAsync(QueueCreateDto dto)
    {
        if (await _db.Queues.AnyAsync(q => q.CustomerId == dto.CustomerId && q.Name == dto.Name))
            return (false, null, "Bu firmada ayni isimde kuyruk zaten var.");

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

        return (true, queue.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, QueueUpdateDto dto)
    {
        var queue = await _db.Queues.FindAsync(id);
        if (queue == null) return (false, "Kuyruk bulunamadi.");

        if (await _db.Queues.AnyAsync(q => q.CustomerId == queue.CustomerId && q.Name == dto.Name && q.Id != id))
            return (false, "Bu firmada ayni isimde kuyruk zaten var.");

        queue.Name = dto.Name;
        queue.Description = dto.Description;
        queue.MaxWaitTimeSeconds = dto.MaxWaitTimeSeconds;
        queue.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var queue = await _db.Queues.FindAsync(id);
        if (queue == null) return (false, "Kuyruk bulunamadi.");

        queue.IsActive = false;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AssignAgentAsync(int queueId, QueueAgentAssignDto dto)
    {
        var queue = await _db.Queues.FindAsync(queueId);
        if (queue == null) return (false, "Kuyruk bulunamadi.");

        var agent = await _db.Users.FindAsync(dto.AgentId);
        if (agent == null) return (false, "Temsilci bulunamadi.");

        if (await _db.QueueAgents.AnyAsync(qa => qa.QueueId == queueId && qa.AgentId == dto.AgentId))
            return (false, "Temsilci zaten bu kuyruga atanmis.");

        _db.QueueAgents.Add(new QueueAgent
        {
            QueueId = queueId,
            AgentId = dto.AgentId
        });
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveAgentAsync(int queueId, int agentId)
    {
        var qa = await _db.QueueAgents.FirstOrDefaultAsync(x => x.QueueId == queueId && x.AgentId == agentId);
        if (qa == null) return (false, "Atama bulunamadi.");

        _db.QueueAgents.Remove(qa);
        await _db.SaveChangesAsync();

        return (true, null);
    }
}

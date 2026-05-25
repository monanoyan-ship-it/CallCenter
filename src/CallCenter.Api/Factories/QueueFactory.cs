using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class QueueFactory : IQueueFactory
{
    private readonly IQueueEntityService _queues;
    private readonly IUserEntityService _users;
    private readonly IUnitOfWork _uow;

    public QueueFactory(IQueueEntityService queues, IUserEntityService users, IUnitOfWork uow)
    {
        _queues = queues;
        _users = users;
        _uow = uow;
    }

    public async Task<PagedResult<QueueListDto>> GetAllAsync(int page, int pageSize, int? customerId, string? search)
    {
        var query = _queues.GetAllQueryable().Include(q => q.Customer).AsQueryable();

        if (customerId.HasValue && customerId.Value > 0)
            query = query.Where(q => q.CustomerId == customerId.Value);

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

    public async Task<QueueDetailDto?> GetByIdAsync(int id, int? customerId = null)
    {
        var query = _queues.GetAllQueryable()
            .Include(q => q.Customer)
            .Include(q => q.QueueAgents)
                .ThenInclude(qa => qa.Agent)
            .Where(q => q.Id == id);

        if (customerId.HasValue)
            query = query.Where(q => q.CustomerId == customerId.Value);

        var q = await query.FirstOrDefaultAsync();
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
        if (await _queues.ExistsByNameAndCustomerAsync(dto.CustomerId, dto.Name))
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

        _queues.Add(queue);
        await _uow.SaveChangesAsync();

        return (true, queue.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, QueueUpdateDto dto, int? customerId = null)
    {
        var queue = await GetScopedQueueAsync(id, customerId);
        if (queue == null) return (false, "Kuyruk bulunamadi.");

        if (await _queues.ExistsByNameAndCustomerAsync(queue.CustomerId, dto.Name, id))
            return (false, "Bu firmada ayni isimde kuyruk zaten var.");

        queue.Name = dto.Name;
        queue.Description = dto.Description;
        queue.MaxWaitTimeSeconds = dto.MaxWaitTimeSeconds;
        queue.IsActive = dto.IsActive;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, int? customerId = null)
    {
        var queue = await GetScopedQueueAsync(id, customerId);
        if (queue == null) return (false, "Kuyruk bulunamadi.");

        queue.IsActive = false;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AssignAgentAsync(int queueId, QueueAgentAssignDto dto, int? customerId = null)
    {
        var queue = await GetScopedQueueAsync(queueId, customerId);
        if (queue == null) return (false, "Kuyruk bulunamadi.");

        var agent = await _users.GetAllQueryable()
            .Include(u => u.CustomerPersonnel)
            .FirstOrDefaultAsync(u => u.Id == dto.AgentId);
        if (agent == null) return (false, "Temsilci bulunamadi.");
        if (agent.CustomerPersonnel?.CustomerId != queue.CustomerId)
            return (false, "Temsilci bu firmaya ait degil.");

        if (await _queues.GetQueueAgentAsync(queueId, dto.AgentId) != null)
            return (false, "Temsilci zaten bu kuyruga atanmis.");

        _queues.AddQueueAgent(new QueueAgent
        {
            QueueId = queueId,
            AgentId = dto.AgentId
        });
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveAgentAsync(int queueId, int agentId, int? customerId = null)
    {
        var queue = await GetScopedQueueAsync(queueId, customerId);
        if (queue == null) return (false, "Kuyruk bulunamadi.");

        var qa = await _queues.GetQueueAgentAsync(queueId, agentId);
        if (qa == null) return (false, "Atama bulunamadi.");

        _queues.RemoveQueueAgent(qa);
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    private Task<Queue?> GetScopedQueueAsync(int id, int? customerId)
    {
        var query = _queues.GetAllQueryable().Where(q => q.Id == id);
        if (customerId.HasValue)
            query = query.Where(q => q.CustomerId == customerId.Value);
        return query.FirstOrDefaultAsync();
    }
}

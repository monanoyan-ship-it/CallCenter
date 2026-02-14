using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogListDto>> GetAllAsync(
        int page, int pageSize,
        string? category = null, string? action = null,
        string? search = null,
        DateTime? dateFrom = null, DateTime? dateTo = null,
        int? customerId = null)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        // Filtreler
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(a => a.Category == category);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a =>
                (a.UserName != null && a.UserName.Contains(search)) ||
                a.Description.Contains(search));

        if (dateFrom.HasValue)
            query = query.Where(a => a.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
        {
            // dateTo gunun sonuna kadar dahil
            var endOfDay = dateTo.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedAt < endOfDay);
        }

        if (customerId.HasValue)
            query = query.Where(a => a.CustomerId == customerId.Value);

        // Sira: En yeni en ustte
        query = query.OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogListDto
            {
                Id = a.Id,
                Category = a.Category,
                Action = a.Action,
                UserName = a.UserName,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Description = a.Description,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt,
                CustomerId = a.CustomerId
            })
            .ToListAsync();

        return new PagedResult<AuditLogListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AuditLogDetailDto?> GetByIdAsync(long id)
    {
        return await _db.AuditLogs.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AuditLogDetailDto
            {
                Id = a.Id,
                Category = a.Category,
                Action = a.Action,
                UserId = a.UserId,
                UserName = a.UserName,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Description = a.Description,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                CreatedAt = a.CreatedAt,
                CustomerId = a.CustomerId
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        return await _db.AuditLogs.AsNoTracking()
            .Select(a => a.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<List<string>> GetActionsAsync(string? category = null)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(a => a.Category == category);

        return await query
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }
}

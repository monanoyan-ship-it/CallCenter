using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class AuditLogFactory : IAuditLogFactory
{
    private readonly IAuditLogEntityService _auditEs;

    public AuditLogFactory(IAuditLogEntityService auditEs)
    {
        _auditEs = auditEs;
    }

    public async Task<PagedResult<AuditLogListDto>> GetAllAsync(
        int page,
        int pageSize,
        string? category = null,
        string? action = null,
        string? search = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? customerId = null)
    {
        var query = ApplyFilters(_auditEs.GetAllQueryable().AsNoTracking(), category, action, search, dateFrom, dateTo, customerId)
            .OrderByDescending(a => a.CreatedAt);

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
        var detail = await _auditEs.GetAllQueryable().AsNoTracking()
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

        if (detail == null) return null;

        detail.OldValues = MaskAuditJson(detail.OldValues);
        detail.NewValues = MaskAuditJson(detail.NewValues);

        return detail;
    }

    public async Task<byte[]> ExportCsvAsync(
        string? category = null,
        string? action = null,
        string? search = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? customerId = null)
    {
        var rows = await ApplyFilters(_auditEs.GetAllQueryable().AsNoTracking(), category, action, search, dateFrom, dateTo, customerId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10000)
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

        var sb = new StringBuilder();
        sb.AppendLine("Id,Tarih,Kategori,Aksiyon,Kullanici,Aciklama,Entity,EntityId,IP,CustomerId");

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                Csv(row.Id),
                Csv(row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(row.Category),
                Csv(row.Action),
                Csv(row.UserName),
                Csv(row.Description),
                Csv(row.EntityType),
                Csv(row.EntityId),
                Csv(row.IpAddress),
                Csv(row.CustomerId)));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        return await _auditEs.GetAllQueryable().AsNoTracking()
            .Select(a => a.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<List<string>> GetActionsAsync(string? category = null)
    {
        var query = _auditEs.GetAllQueryable().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(a => a.Category == category);

        return await query
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }

    private static IQueryable<AuditLog> ApplyFilters(
        IQueryable<AuditLog> query,
        string? category,
        string? action,
        string? search,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? customerId)
    {
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
            var endOfDay = dateTo.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedAt < endOfDay);
        }

        if (customerId.HasValue)
            query = query.Where(a => a.CustomerId == customerId.Value);

        return query;
    }

    private static string Csv(object? value)
    {
        var text = Convert.ToString(value) ?? "";
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private static string? MaskAuditJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        try
        {
            var node = JsonNode.Parse(json);
            if (node == null) return json;
            MaskNode(node);
            return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            return json;
        }
    }

    private static void MaskNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (IsSensitiveKey(property.Key))
                    obj[property.Key] = "***";
                else if (property.Value != null)
                    MaskNode(property.Value);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
                if (item != null) MaskNode(item);
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        var lower = key.ToLowerInvariant();
        return lower.Contains("password") ||
               lower.Contains("token") ||
               lower.Contains("secret") ||
               lower.Contains("credential") ||
               lower.Contains("apikey") ||
               lower.Contains("api_key") ||
               lower.Contains("merchantkey") ||
               lower.Contains("merchantsalt") ||
               lower.Contains("clientsecret") ||
               lower.Contains("iban") ||
               lower.Contains("hash");
    }
}

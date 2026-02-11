using System.Security.Claims;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class SipAccountService : ISipAccountService
{
    private readonly AppDbContext _db;

    public SipAccountService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SipConnectionInfoDto?> GetMyConnectionAsync(int customerId, string displayName)
    {
        var sip = await _db.SipAccounts
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.IsDefault && s.IsActive);

        sip ??= await _db.SipAccounts
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.IsActive);

        if (sip == null) return null;

        var domain = sip.Domain ?? sip.Server;

        // WsUri: Ozel tanimlanmissa onu kullan, yoksa heuristic ile olustur
        string wsUri;
        if (!string.IsNullOrWhiteSpace(sip.WsUri))
        {
            wsUri = sip.WsUri;
        }
        else
        {
            var wsPort = sip.Transport?.ToUpper() == "WSS" ? sip.Port : 8089;
            wsUri = $"wss://{sip.Server}:{wsPort}/ws";
        }

        return new SipConnectionInfoDto
        {
            WsUri = wsUri,
            SipUri = $"sip:{sip.Username}@{domain}",
            AuthUsername = sip.Username,
            AuthPassword = sip.Password,
            DisplayName = displayName,
            Transport = "WSS",
            UseSrtp = sip.UseSrtp
        };
    }

    public async Task<PagedResult<SipAccountListDto>> GetAllAsync(int page, int pageSize, int? customerId)
    {
        var query = _db.SipAccounts.Include(s => s.Customer).AsQueryable();

        if (customerId.HasValue && customerId.Value > 0)
        {
            query = query.Where(s => s.CustomerId == customerId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(s => s.Customer.Name).ThenBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SipAccountListDto
            {
                Id = s.Id,
                Name = s.Name,
                Server = s.Server,
                Port = s.Port,
                Username = s.Username,
                Transport = s.Transport,
                WsUri = s.WsUri,
                IsDefault = s.IsDefault,
                IsActive = s.IsActive,
                CustomerId = s.CustomerId,
                CustomerName = s.Customer.Name,
                OrganizationUnitId = s.OrganizationUnitId,
                OrganizationUnitName = s.OrganizationUnit != null ? s.OrganizationUnit.Name : null
            })
            .ToListAsync();

        return new PagedResult<SipAccountListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<object?> GetByIdAsync(int id)
    {
        var s = await _db.SipAccounts.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return null;

        return new
        {
            s.Id,
            s.Name,
            s.Server,
            s.Port,
            s.Domain,
            s.Username,
            Password = "********",
            s.Transport,
            s.WsUri,
            s.UseSrtp,
            s.IsDefault,
            s.IsActive,
            s.CustomerId,
            CustomerName = s.Customer.Name
        };
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateAsync(SipAccountCreateDto dto)
    {
        if (dto.IsDefault)
        {
            var existingDefault = await _db.SipAccounts
                .FirstOrDefaultAsync(s => s.CustomerId == dto.CustomerId && s.IsDefault && s.IsActive);
            if (existingDefault != null)
            {
                existingDefault.IsDefault = false;
            }
        }

        var sip = new SipAccount
        {
            Name = dto.Name,
            Server = dto.Server,
            Port = dto.Port,
            Domain = dto.Domain,
            Username = dto.Username,
            Password = dto.Password,
            Transport = dto.Transport,
            WsUri = dto.WsUri,
            UseSrtp = dto.UseSrtp,
            IsDefault = dto.IsDefault,
            IsActive = true,
            CustomerId = dto.CustomerId,
            CreatedAt = DateTime.UtcNow
        };

        _db.SipAccounts.Add(sip);
        await _db.SaveChangesAsync();

        return (true, sip.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, SipAccountUpdateDto dto)
    {
        var sip = await _db.SipAccounts.FindAsync(id);
        if (sip == null) return (false, "SIP hesabi bulunamadi.");

        if (dto.IsDefault && !sip.IsDefault)
        {
            var existingDefault = await _db.SipAccounts
                .FirstOrDefaultAsync(s => s.CustomerId == sip.CustomerId && s.IsDefault && s.IsActive && s.Id != id);
            if (existingDefault != null)
            {
                existingDefault.IsDefault = false;
            }
        }

        sip.Name = dto.Name;
        sip.Server = dto.Server;
        sip.Port = dto.Port;
        sip.Domain = dto.Domain;
        sip.Username = dto.Username;
        sip.Transport = dto.Transport;
        sip.WsUri = dto.WsUri;
        sip.UseSrtp = dto.UseSrtp;
        sip.IsDefault = dto.IsDefault;
        sip.IsActive = dto.IsActive;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            sip.Password = dto.Password;
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var sip = await _db.SipAccounts.FindAsync(id);
        if (sip == null) return (false, "SIP hesabi bulunamadi.");

        sip.IsActive = false;
        await _db.SaveChangesAsync();

        return (true, null);
    }
}

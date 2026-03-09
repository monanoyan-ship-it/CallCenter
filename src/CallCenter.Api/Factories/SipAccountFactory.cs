using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SipAccountFactory : ISipAccountFactory
{
    private readonly ISipAccountEntityService _sipEs;
    private readonly ISipLineEntityService _lineEs;
    private readonly AesEncryptionService _encryption;
    private readonly IUnitOfWork _uow;

    public SipAccountFactory(
        ISipAccountEntityService sipEs,
        ISipLineEntityService lineEs,
        AesEncryptionService encryption,
        IUnitOfWork uow)
    {
        _sipEs = sipEs;
        _lineEs = lineEs;
        _encryption = encryption;
        _uow = uow;
    }

    // ═══════════════════════════════════════════════════════════════
    // OPERATOR HAT TAHSISI
    // ═══════════════════════════════════════════════════════════════

    public async Task<SipConnectionInfoDto?> GetMyConnectionAsync(int customerId, int? personnelId, string displayName)
    {
        SipLine? line = null;

        // 1. Bu personele zaten tahsis edilmis hat var mi? (idempotent)
        if (personnelId.HasValue)
            line = await _lineEs.GetByPersonnelAsync(personnelId.Value);

        // 2. Yoksa bos hat bul ve dinamik tahsis et
        if (line == null && personnelId.HasValue)
        {
            line = await _lineEs.AcquireUnassignedAsync(customerId);
            if (line != null)
            {
                line.AssignedPersonnelId = personnelId.Value;
                line.AssignedAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync();
            }
        }

        // 3. Hat bulunamadiysa — tum hatlar dolu (fallback YOK)
        if (line == null) return null;

        return BuildConnectionDto(line, displayName);
    }

    public async Task ReleaseLineAsync(int personnelId)
    {
        await _lineEs.ReleaseByPersonnelAsync(personnelId);
        await _uow.SaveChangesAsync();
    }

    private SipConnectionInfoDto BuildConnectionDto(SipLine line, string displayName)
    {
        var gw = line.SipAccount;
        var domain = gw.Domain ?? gw.Server;

        string wsUri;
        if (!string.IsNullOrWhiteSpace(gw.WsUri))
        {
            wsUri = gw.WsUri;
        }
        else
        {
            var wsPort = gw.Transport?.ToUpper() == "WSS" ? gw.Port : 8089;
            wsUri = $"wss://{gw.Server}:{wsPort}/ws";
        }

        return new SipConnectionInfoDto
        {
            SipLineId = line.Id,
            SipAccountId = gw.Id,
            WsUri = wsUri,
            SipUri = $"sip:{line.Username}@{domain}",
            AuthUsername = line.Username,
            AuthPassword = _encryption.Decrypt(line.Password),
            DisplayName = displayName,
            Transport = gw.Transport ?? "UDP",
            UseSrtp = gw.UseSrtp,
            StunServer = gw.StunServer,
            TurnServer = gw.TurnServer,
            TurnUsername = gw.TurnUsername,
            TurnPassword = !string.IsNullOrWhiteSpace(gw.TurnPassword)
                ? _encryption.Decrypt(gw.TurnPassword)
                : null,
            PreferredCodecs = gw.PreferredCodecs,
            JitterBufferMinMs = gw.JitterBufferMinMs,
            JitterBufferMaxMs = gw.JitterBufferMaxMs
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // ADMIN: GATEWAY CRUD
    // ═══════════════════════════════════════════════════════════════

    public async Task<PagedResult<SipAccountListDto>> GetAllAsync(int page, int pageSize, int? customerId)
    {
        var query = _sipEs.GetAllQueryable()
            .Include(s => s.Customer)
            .Include(s => s.Lines)
            .AsQueryable();

        if (customerId.HasValue && customerId.Value > 0)
            query = query.Where(s => s.CustomerId == customerId.Value);

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
                Transport = s.Transport,
                WsUri = s.WsUri,
                UseSrtp = s.UseSrtp,
                StunServer = s.StunServer,
                TurnServer = s.TurnServer,
                IsDefault = s.IsDefault,
                IsActive = s.IsActive,
                CustomerId = s.CustomerId,
                CustomerName = s.Customer.Name,
                OrganizationUnitId = s.OrganizationUnitId,
                OrganizationUnitName = s.OrganizationUnit != null ? s.OrganizationUnit.Name : null,
                PreferredCodecs = s.PreferredCodecs,
                JitterBufferMinMs = s.JitterBufferMinMs,
                JitterBufferMaxMs = s.JitterBufferMaxMs,
                LineCount = s.Lines.Count,
                ActiveLineCount = s.Lines.Count(l => l.IsActive)
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
        var s = await _sipEs.GetByIdWithCustomerAsync(id);
        if (s == null) return null;

        var lines = await _lineEs.GetAllQueryable()
            .Where(l => l.SipAccountId == id)
            .Select(l => new
            {
                l.Id,
                l.ChannelNumber,
                l.Username,
                l.Description,
                l.IsActive,
                l.AssignedPersonnelId
            })
            .ToListAsync();

        return new
        {
            s.Id,
            s.Name,
            s.Server,
            s.Port,
            s.Domain,
            s.Transport,
            s.WsUri,
            s.UseSrtp,
            s.StunServer,
            s.TurnServer,
            s.TurnUsername,
            TurnPassword = "********",
            s.PreferredCodecs,
            s.JitterBufferMinMs,
            s.JitterBufferMaxMs,
            s.IsDefault,
            s.IsActive,
            s.CustomerId,
            CustomerName = s.Customer.Name,
            Lines = lines
        };
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateAsync(SipAccountCreateDto dto)
    {
        if (dto.IsDefault)
        {
            var existingDefault = await _sipEs.GetExistingDefaultAsync(dto.CustomerId);
            if (existingDefault != null)
                existingDefault.IsDefault = false;
        }

        var sip = new SipAccount
        {
            Name = dto.Name,
            Server = dto.Server,
            Port = dto.Port,
            Domain = dto.Domain,
            Transport = dto.Transport,
            WsUri = dto.WsUri,
            UseSrtp = dto.UseSrtp,
            StunServer = dto.StunServer,
            TurnServer = dto.TurnServer,
            TurnUsername = dto.TurnUsername,
            TurnPassword = !string.IsNullOrWhiteSpace(dto.TurnPassword)
                ? _encryption.Encrypt(dto.TurnPassword) : null,
            PreferredCodecs = dto.PreferredCodecs,
            JitterBufferMinMs = dto.JitterBufferMinMs,
            JitterBufferMaxMs = dto.JitterBufferMaxMs,
            IsDefault = dto.IsDefault,
            IsActive = true,
            CustomerId = dto.CustomerId,
            CreatedAt = DateTime.UtcNow
        };

        // Gateway ile birlikte hatlar da eklenebilir
        if (dto.Lines?.Count > 0)
        {
            foreach (var lineDto in dto.Lines)
            {
                sip.Lines.Add(new SipLine
                {
                    ChannelNumber = lineDto.ChannelNumber,
                    Username = lineDto.Username,
                    Password = _encryption.Encrypt(lineDto.Password),
                    Description = lineDto.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        _sipEs.Add(sip);
        await _uow.SaveChangesAsync();

        return (true, sip.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, SipAccountUpdateDto dto)
    {
        var sip = await _sipEs.GetByIdAsync(id);
        if (sip == null) return (false, "Gateway bulunamadi.");

        if (dto.IsDefault && !sip.IsDefault)
        {
            var existingDefault = await _sipEs.GetExistingDefaultAsync(sip.CustomerId, excludeId: id);
            if (existingDefault != null)
                existingDefault.IsDefault = false;
        }

        sip.Name = dto.Name;
        sip.Server = dto.Server;
        sip.Port = dto.Port;
        sip.Domain = dto.Domain;
        sip.Transport = dto.Transport;
        sip.WsUri = dto.WsUri;
        sip.UseSrtp = dto.UseSrtp;
        sip.StunServer = dto.StunServer;
        sip.TurnServer = dto.TurnServer;
        sip.TurnUsername = dto.TurnUsername;
        if (!string.IsNullOrWhiteSpace(dto.TurnPassword))
            sip.TurnPassword = _encryption.Encrypt(dto.TurnPassword);
        sip.PreferredCodecs = dto.PreferredCodecs;
        sip.JitterBufferMinMs = dto.JitterBufferMinMs;
        sip.JitterBufferMaxMs = dto.JitterBufferMaxMs;
        sip.IsDefault = dto.IsDefault;
        sip.IsActive = dto.IsActive;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var sip = await _sipEs.GetByIdAsync(id);
        if (sip == null) return (false, "Gateway bulunamadi.");

        sip.IsActive = false;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════════
    // ADMIN: LINE CRUD
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<SipLineListDto>> GetLinesByGatewayAsync(int gatewayId)
    {
        return await _lineEs.GetAllQueryable()
            .Include(l => l.SipAccount)
            .Include(l => l.AssignedPersonnel).ThenInclude(p => p!.User)
            .Where(l => l.SipAccountId == gatewayId)
            .OrderBy(l => l.ChannelNumber)
            .Select(l => new SipLineListDto
            {
                Id = l.Id,
                ChannelNumber = l.ChannelNumber,
                Username = l.Username,
                Description = l.Description,
                IsActive = l.IsActive,
                SipAccountId = l.SipAccountId,
                GatewayName = l.SipAccount.Name,
                AssignedPersonnelId = l.AssignedPersonnelId,
                AssignedPersonnelName = l.AssignedPersonnel != null ? l.AssignedPersonnel.User.FullName : null
            })
            .ToListAsync();
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateLineAsync(int gatewayId, SipLineCreateDto dto)
    {
        var gw = await _sipEs.GetByIdAsync(gatewayId);
        if (gw == null) return (false, null, "Gateway bulunamadi.");

        var line = new SipLine
        {
            SipAccountId = gatewayId,
            ChannelNumber = dto.ChannelNumber,
            Username = dto.Username,
            Password = _encryption.Encrypt(dto.Password),
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _lineEs.Add(line);
        await _uow.SaveChangesAsync();

        return (true, line.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateLineAsync(int lineId, SipLineUpdateDto dto)
    {
        var line = await _lineEs.GetByIdAsync(lineId);
        if (line == null) return (false, "Hat bulunamadi.");

        line.ChannelNumber = dto.ChannelNumber;
        line.IsActive = dto.IsActive;

        if (!string.IsNullOrWhiteSpace(dto.Username))
            line.Username = dto.Username;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            line.Password = _encryption.Encrypt(dto.Password);

        if (dto.Description != null)
            line.Description = dto.Description;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteLineAsync(int lineId)
    {
        var line = await _lineEs.GetByIdAsync(lineId);
        if (line == null) return (false, "Hat bulunamadi.");

        line.IsActive = false;
        // Tahsis varsa serbest birak
        line.AssignedPersonnelId = null;
        line.AssignedAt = null;

        await _uow.SaveChangesAsync();
        return (true, null);
    }
}

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
    private readonly AesEncryptionService _encryption;
    private readonly IUnitOfWork _uow;

    public SipAccountFactory(ISipAccountEntityService sipEs, AesEncryptionService encryption, IUnitOfWork uow)
    {
        _sipEs = sipEs;
        _encryption = encryption;
        _uow = uow;
    }

    public async Task<SipConnectionInfoDto?> GetMyConnectionAsync(int customerId, int? personnelId, string displayName)
    {
        // 1. Bu personele zaten atanmis hesap var mi?
        SipAccount? sip = null;
        if (personnelId.HasValue)
            sip = await _sipEs.GetByPersonnelAsync(personnelId.Value);

        // 2. Yoksa atanmamis bos bir hat bul ve otomatik ata
        if (sip == null && personnelId.HasValue)
        {
            sip = await _sipEs.GetFirstUnassignedAsync(customerId);
            if (sip != null)
            {
                sip.AssignedPersonnelId = personnelId.Value;
                await _uow.SaveChangesAsync();
            }
        }

        // 3. Hala yoksa firma default veya ilk aktifi dene (fallback)
        sip ??= await _sipEs.GetDefaultByCustomerAsync(customerId);
        sip ??= await _sipEs.GetFirstActiveByCustomerAsync(customerId);

        if (sip == null) return null;

        var domain = sip.Domain ?? sip.Server;

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
            AuthPassword = _encryption.Decrypt(sip.Password),
            DisplayName = displayName,
            Transport = "WSS",
            UseSrtp = sip.UseSrtp,
            StunServer = sip.StunServer,
            TurnServer = sip.TurnServer,
            TurnUsername = sip.TurnUsername,
            TurnPassword = !string.IsNullOrWhiteSpace(sip.TurnPassword)
                ? _encryption.Decrypt(sip.TurnPassword)
                : null,
            PreferredCodecs = sip.PreferredCodecs,
            JitterBufferMinMs = sip.JitterBufferMinMs,
            JitterBufferMaxMs = sip.JitterBufferMaxMs
        };
    }

    public async Task<PagedResult<SipAccountListDto>> GetAllAsync(int page, int pageSize, int? customerId)
    {
        var query = _sipEs.GetAllQueryable().Include(s => s.Customer).Include(s => s.AssignedPersonnel).ThenInclude(p => p!.User).AsQueryable();

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
                UseSrtp = s.UseSrtp,
                StunServer = s.StunServer,
                TurnServer = s.TurnServer,
                IsDefault = s.IsDefault,
                IsActive = s.IsActive,
                CustomerId = s.CustomerId,
                CustomerName = s.Customer.Name,
                OrganizationUnitId = s.OrganizationUnitId,
                OrganizationUnitName = s.OrganizationUnit != null ? s.OrganizationUnit.Name : null,
                AssignedPersonnelId = s.AssignedPersonnelId,
                AssignedPersonnelName = s.AssignedPersonnel != null ? s.AssignedPersonnel.User.FullName : null,
                PreferredCodecs = s.PreferredCodecs,
                JitterBufferMinMs = s.JitterBufferMinMs,
                JitterBufferMaxMs = s.JitterBufferMaxMs
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
            s.AssignedPersonnelId
        };
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateAsync(SipAccountCreateDto dto)
    {
        if (dto.IsDefault)
        {
            var existingDefault = await _sipEs.GetExistingDefaultAsync(dto.CustomerId);
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
            Password = _encryption.Encrypt(dto.Password),
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
            AssignedPersonnelId = dto.AssignedPersonnelId,
            CreatedAt = DateTime.UtcNow
        };

        _sipEs.Add(sip);
        await _uow.SaveChangesAsync();

        return (true, sip.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, SipAccountUpdateDto dto)
    {
        var sip = await _sipEs.GetByIdAsync(id);
        if (sip == null) return (false, "SIP hesabi bulunamadi.");

        if (dto.IsDefault && !sip.IsDefault)
        {
            var existingDefault = await _sipEs.GetExistingDefaultAsync(sip.CustomerId, excludeId: id);
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
        sip.StunServer = dto.StunServer;
        sip.TurnServer = dto.TurnServer;
        sip.TurnUsername = dto.TurnUsername;
        if (!string.IsNullOrWhiteSpace(dto.TurnPassword))
        {
            sip.TurnPassword = _encryption.Encrypt(dto.TurnPassword);
        }
        sip.PreferredCodecs = dto.PreferredCodecs;
        sip.JitterBufferMinMs = dto.JitterBufferMinMs;
        sip.JitterBufferMaxMs = dto.JitterBufferMaxMs;
        sip.IsDefault = dto.IsDefault;
        sip.IsActive = dto.IsActive;
        sip.AssignedPersonnelId = dto.AssignedPersonnelId;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            sip.Password = _encryption.Encrypt(dto.Password);
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var sip = await _sipEs.GetByIdAsync(id);
        if (sip == null) return (false, "SIP hesabi bulunamadi.");

        sip.IsActive = false;
        await _uow.SaveChangesAsync();

        return (true, null);
    }
}

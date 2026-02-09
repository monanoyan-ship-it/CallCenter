using System.Security.Claims;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SipAccountsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SipAccountsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Agent'ın müşterisinin default SIP hesabının bağlantı bilgilerini döndürür.
    /// WebSocket URI otomatik oluşturulur: wss://server:port/ws
    /// Tüm authenticated kullanıcılar erişebilir.
    /// </summary>
    [HttpGet("my/connection")]
    public async Task<ActionResult<SipConnectionInfoDto>> GetMyConnection()
    {
        var customerIdClaim = User.FindFirstValue("CustomerId");
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
        {
            return BadRequest(new { message = "Müşteri bilgisi bulunamadı. Lütfen tekrar giriş yapın." });
        }

        var sip = await _db.SipAccounts
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.IsDefault && s.IsActive);

        // Default yoksa herhangi bir aktif hesap al
        sip ??= await _db.SipAccounts
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.IsActive);

        if (sip == null)
        {
            return NotFound(new { message = "Firmanıza ait aktif SIP hesabı bulunamadı." });
        }

        var domain = sip.Domain ?? sip.Server;
        var userName = User.FindFirstValue(ClaimTypes.GivenName) ?? sip.Username;

        // WebSocket URI: SIP.js sadece WSS ile çalışır
        // Yaygın portlar: 8089 (Asterisk), 7443 (FreeSWITCH), 443 (Telnyx/cloud)
        var wsPort = sip.Transport?.ToUpper() == "WSS" ? sip.Port : 8089;
        var wsUri = $"wss://{sip.Server}:{wsPort}/ws";

        return Ok(new SipConnectionInfoDto
        {
            WsUri = wsUri,
            SipUri = $"sip:{sip.Username}@{domain}",
            AuthUsername = sip.Username,
            AuthPassword = sip.Password,
            DisplayName = userName,
            Transport = "WSS",
            UseSrtp = sip.UseSrtp
        });
    }

    /// <summary>Sayfalamali SIP hesap listesi (Password yok)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<SipAccountListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? customerId = null)
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
                IsDefault = s.IsDefault,
                IsActive = s.IsActive,
                CustomerId = s.CustomerId,
                CustomerName = s.Customer.Name
            })
            .ToListAsync();

        return Ok(new PagedResult<SipAccountListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>SIP hesap detay (Password maskelenmis)</summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetById(int id)
    {
        var s = await _db.SipAccounts.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound(new { message = "SIP hesabi bulunamadi." });

        return Ok(new
        {
            s.Id,
            s.Name,
            s.Server,
            s.Port,
            s.Domain,
            s.Username,
            Password = "********",
            s.Transport,
            s.UseSrtp,
            s.IsDefault,
            s.IsActive,
            s.CustomerId,
            CustomerName = s.Customer.Name
        });
    }

    /// <summary>Yeni SIP hesap olustur</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create(SipAccountCreateDto dto)
    {
        // IsDefault — ayni firma icinde tek default olacak
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
            UseSrtp = dto.UseSrtp,
            IsDefault = dto.IsDefault,
            IsActive = true,
            CustomerId = dto.CustomerId,
            CreatedAt = DateTime.UtcNow
        };

        _db.SipAccounts.Add(sip);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = sip.Id }, new { id = sip.Id });
    }

    /// <summary>SIP hesap guncelle (Password null ise degismez)</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(int id, SipAccountUpdateDto dto)
    {
        var sip = await _db.SipAccounts.FindAsync(id);
        if (sip == null) return NotFound(new { message = "SIP hesabi bulunamadi." });

        // IsDefault kontrolu
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
        sip.UseSrtp = dto.UseSrtp;
        sip.IsDefault = dto.IsDefault;
        sip.IsActive = dto.IsActive;

        // Password — sadece dolu ise guncelle
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            sip.Password = dto.Password;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>SIP hesap sil (soft delete)</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var sip = await _db.SipAccounts.FindAsync(id);
        if (sip == null) return NotFound(new { message = "SIP hesabi bulunamadi." });

        sip.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

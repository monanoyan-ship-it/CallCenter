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
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Sayfalamali musteri listesi</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = _db.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(s)
                                  || (c.Email != null && c.Email.ToLower().Contains(s))
                                  || (c.Phone != null && c.Phone.Contains(s)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Email = c.Email,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                PersonnelCount = c.Personnel.Count,
                QueueCount = c.Queues.Count,
                SipAccountCount = c.SipAccounts.Count
            })
            .ToListAsync();

        return Ok(new PagedResult<CustomerListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>Musteri detay (personel listesi dahil)</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(int id)
    {
        var c = await _db.Customers
            .Include(x => x.Personnel)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c == null) return NotFound(new { message = "Musteri bulunamadi." });

        return Ok(new CustomerDetailDto
        {
            Id = c.Id,
            Name = c.Name,
            TaxNumber = c.TaxNumber,
            Address = c.Address,
            Phone = c.Phone,
            Email = c.Email,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            Personnel = c.Personnel.Select(p => new PersonnelSimpleDto
            {
                Id = p.Id,
                FullName = p.User.FullName,
                Title = p.Title
            }).ToList()
        });
    }

    /// <summary>Yeni musteri olustur (varsayilan portal modullerini otomatik ata)</summary>
    [HttpPost]
    public async Task<ActionResult> Create(CustomerCreateDto dto)
    {
        var customer = new Customer
        {
            Name = dto.Name,
            TaxNumber = dto.TaxNumber,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        // Varsayilan portal modullerini otomatik ata
        foreach (var module in PortalModules.Defaults)
        {
            _db.CustomerPortalModules.Add(new CustomerPortalModule
            {
                CustomerId = customer.Id,
                ModuleId = module.Id,
                IsActive = true,
                ActivatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, new { id = customer.Id });
    }

    /// <summary>Musteri guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, CustomerUpdateDto dto)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = "Musteri bulunamadi." });

        customer.Name = dto.Name;
        customer.TaxNumber = dto.TaxNumber;
        customer.Address = dto.Address;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Musteri sil (soft delete)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = "Musteri bulunamadi." });

        customer.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

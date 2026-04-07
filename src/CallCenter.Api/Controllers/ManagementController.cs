using CallCenter.Api.Factories.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ManagementController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISupervisorFactory? _supervisorFactory;

    public ManagementController(AppDbContext db, IServiceProvider sp)
    {
        _db = db;
        _supervisorFactory = sp.GetService<ISupervisorFactory>();
    }

    [HttpGet("product-types")]
    public IActionResult GetProductTypes()
    {
        var types = ProductTypes.All.Select(t => new { t.Id, t.SystemName, t.Description, t.Icon, t.CssClass }).ToList();
        return Ok(types);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ManagementDashboardDto>> GetDashboard()
    {
        var customers = await _db.Customers.ToListAsync();
        var users = await _db.Users.ToListAsync();
        var customerProducts = await _db.CustomerProducts.Where(cp => cp.IsActive).ToListAsync();

        var dto = new ManagementDashboardDto
        {
            TotalCustomers = customers.Count,
            ActiveCustomers = customers.Count(c => c.IsActive),
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            TotalSalonCustomers = customerProducts
                .Where(cp => cp.ProductTypeId == ProductTypes.Ids.Salon)
                .Select(cp => cp.CustomerId).Distinct().Count(),
            TotalCallCenterCustomers = customerProducts
                .Where(cp => cp.ProductTypeId == ProductTypes.Ids.CallCenter)
                .Select(cp => cp.CustomerId).Distinct().Count(),
        };

        // Cagri istatistikleri (supervisor factory varsa)
        if (_supervisorFactory != null)
        {
            try
            {
                var dashboard = await _supervisorFactory.GetDashboardAsync(null);
                dto.OnlineAgentCount = dashboard.AvailableAgentCount;
                dto.ActiveCallCount = dashboard.ActiveCallCount;
                dto.TodayTotalCalls = dashboard.TodayTotalCallCount;
                dto.TodayAnsweredCalls = dashboard.TodayAnsweredCount;
                dto.TodayMissedCalls = dashboard.TodayMissedCount;
            }
            catch { /* supervisor factory hatasi dashboard'u bozmasin */ }
        }

        // Musteri-modul kullanim tablosu
        var modules = await _db.CustomerPortalModules
            .Where(m => m.IsActive)
            .ToListAsync();

        var productsByCustomer = customerProducts
            .GroupBy(cp => cp.CustomerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        dto.CustomerModules = customers.Select(c =>
        {
            var prods = productsByCustomer.GetValueOrDefault(c.Id, new());
            return new CustomerModuleUsageDto
            {
                CustomerId = c.Id,
                CustomerName = c.Name,
                IsActive = c.IsActive,
                ProductTypeIds = prods.Select(p => p.ProductTypeId).ToList(),
                ProductTypeNames = prods.Select(p => ProductTypes.GetById(p.ProductTypeId)?.Description ?? "?").ToList(),
                PersonnelCount = 0,
                ModuleCount = modules.Count(m => m.CustomerId == c.Id),
                ActiveModules = modules.Where(m => m.CustomerId == c.Id)
                    .Select(m => PortalModules.GetById(m.ModuleId)?.SystemName ?? $"Modul-{m.ModuleId}")
                    .ToList()
            };
        }).OrderByDescending(c => c.IsActive).ThenBy(c => c.CustomerName).ToList();

        // Personel sayilarini toplu getir
        var personnelCounts = await _db.CustomerPersonnel
            .Where(p => p.IsActive)
            .GroupBy(p => p.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var pc in personnelCounts)
        {
            var cm = dto.CustomerModules.FirstOrDefault(c => c.CustomerId == pc.CustomerId);
            if (cm != null) cm.PersonnelCount = pc.Count;
        }

        return Ok(dto);
    }

    // ═══ MODULE PRICING ═══

    /// <summary>Tum modul fiyatlarini listele</summary>
    [HttpGet("module-pricing")]
    public async Task<IActionResult> GetModulePricing()
    {
        var pricings = await _db.ModulePricings.ToListAsync();
        var pricingMap = pricings.ToDictionary(p => p.ModuleId);

        var result = SalonPortalModules.All.Select(m =>
        {
            pricingMap.TryGetValue(m.Id, out var pricing);
            var groupId = SalonModuleGroups.GetGroupId(m.Id);
            return new ModulePricingDto
            {
                ModuleId = m.Id,
                ModuleName = m.SystemName,
                Description = m.Description,
                Icon = m.Icon,
                IsDefault = m.IsDefault,
                GroupId = groupId,
                GroupName = SalonModuleGroups.GetById(groupId ?? 0)?.Description,
                MonthlyPrice = pricing?.MonthlyPrice ?? 0,
                HasPricing = pricing != null
            };
        }).OrderBy(x => x.ModuleId).ToList();

        return Ok(result);
    }

    /// <summary>Tek modul fiyat guncelle (upsert)</summary>
    [HttpPost("module-pricing")]
    public async Task<IActionResult> UpdateModulePricing([FromBody] UpdateModulePricingRequest request)
    {
        var existing = await _db.ModulePricings.FirstOrDefaultAsync(p => p.ModuleId == request.ModuleId);
        if (existing != null)
        {
            existing.MonthlyPrice = request.MonthlyPrice;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.ModulePricings.Add(new Shared.Entities.ModulePricing
            {
                ModuleId = request.ModuleId,
                MonthlyPrice = request.MonthlyPrice
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    /// <summary>Toplu modul fiyat guncelle</summary>
    [HttpPost("module-pricing/bulk")]
    public async Task<IActionResult> BulkUpdateModulePricing([FromBody] BulkUpdateModulePricingRequest request)
    {
        var existingPricings = await _db.ModulePricings.ToListAsync();
        var existingMap = existingPricings.ToDictionary(p => p.ModuleId);

        foreach (var price in request.Prices)
        {
            if (existingMap.TryGetValue(price.ModuleId, out var existing))
            {
                existing.MonthlyPrice = price.MonthlyPrice;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.ModulePricings.Add(new Shared.Entities.ModulePricing
                {
                    ModuleId = price.ModuleId,
                    MonthlyPrice = price.MonthlyPrice
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, count = request.Prices.Count });
    }

    // ═══ MODULE REQUESTS (Admin) ═══

    /// <summary>Modul talepleri (varsayilan: sadece bekleyenler, all=true ile tumu)</summary>
    [HttpGet("module-requests")]
    public async Task<IActionResult> GetModuleRequests([FromQuery] bool all = false)
    {
        var query = _db.ModuleRequests
            .Include(r => r.Customer)
            .Include(r => r.RequestedByPersonnel)
            .Include(r => r.ReviewedByUser)
            .AsQueryable();

        if (!all)
            query = query.Where(r => r.StatusId == ModuleRequestStatuses.Ids.Pending);

        var requests = await query
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new ModuleRequestDto
            {
                Id = r.Id,
                Uid = r.Uid,
                CustomerId = r.CustomerId,
                CustomerName = r.Customer.Name,
                ModuleId = r.ModuleId,
                StatusId = r.StatusId,
                RequestNotes = r.RequestNotes,
                AdminNotes = r.AdminNotes,
                RequestedAt = r.RequestedAt,
                ReviewedAt = r.ReviewedAt,
                ReviewedByName = r.ReviewedByUser != null ? r.ReviewedByUser.FullName : null
            })
            .ToListAsync();

        // Modul bilgilerini ekle
        var pricings = await _db.ModulePricings.ToListAsync();
        foreach (var req in requests)
        {
            var module = SalonPortalModules.GetById(req.ModuleId);
            req.ModuleName = module?.SystemName;
            req.ModuleIcon = module?.Icon;
            req.StatusName = ModuleRequestStatuses.GetById(req.StatusId)?.Description;
            req.CatalogPrice = pricings.FirstOrDefault(p => p.ModuleId == req.ModuleId)?.MonthlyPrice;
        }

        return Ok(requests);
    }
}

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

    [HttpGet("dashboard")]
    public async Task<ActionResult<ManagementDashboardDto>> GetDashboard()
    {
        var customers = await _db.Customers.ToListAsync();
        var users = await _db.Users.ToListAsync();

        var dto = new ManagementDashboardDto
        {
            TotalCustomers = customers.Count,
            ActiveCustomers = customers.Count(c => c.IsActive),
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            TotalSalonCustomers = customers.Count(c => c.ProductTypeId == 2 || c.ProductTypeId == 3),
            TotalCallCenterCustomers = customers.Count(c => c.ProductTypeId == 1 || c.ProductTypeId == 3),
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

        var productTypeNames = new Dictionary<int, string>
        {
            { 1, "Call Center" }, { 2, "Salon" }, { 3, "Her Ikisi" }
        };

        dto.CustomerModules = customers.Select(c => new CustomerModuleUsageDto
        {
            CustomerId = c.Id,
            CustomerName = c.Name,
            IsActive = c.IsActive,
            ProductTypeId = c.ProductTypeId,
            ProductTypeName = productTypeNames.GetValueOrDefault(c.ProductTypeId, "Bilinmiyor"),
            PersonnelCount = 0, // asagida dolacak
            ModuleCount = modules.Count(m => m.CustomerId == c.Id),
            ActiveModules = modules.Where(m => m.CustomerId == c.Id)
                .Select(m => PortalModules.GetById(m.ModuleId)?.SystemName ?? $"Modul-{m.ModuleId}")
                .ToList()
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
}

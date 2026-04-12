using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class ManagementFactory : IManagementFactory
{
    private readonly ICustomerEntityService _customers;
    private readonly IUserEntityService _users;
    private readonly ICustomerProductEntityService _products;
    private readonly ICustomerPortalModuleEntityService _modules;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly IModulePricingEntityService _pricing;
    private readonly IModuleRequestEntityService _requests;
    private readonly ISupervisorFactory? _supervisorFactory;
    private readonly IUnitOfWork _uow;

    public ManagementFactory(
        ICustomerEntityService customers,
        IUserEntityService users,
        ICustomerProductEntityService products,
        ICustomerPortalModuleEntityService modules,
        ICustomerPersonnelEntityService personnel,
        IModulePricingEntityService pricing,
        IModuleRequestEntityService requests,
        IUnitOfWork uow,
        IServiceProvider sp)
    {
        _customers = customers;
        _users = users;
        _products = products;
        _modules = modules;
        _personnel = personnel;
        _pricing = pricing;
        _requests = requests;
        _uow = uow;
        _supervisorFactory = sp.GetService<ISupervisorFactory>();
    }

    public async Task<ManagementDashboardDto> GetDashboardAsync()
    {
        var customers = await _customers.GetAllQueryable().ToListAsync();
        var users = await _users.GetAllQueryable().ToListAsync();
        var customerProducts = await _products.GetAllQueryable().Where(cp => cp.IsActive).ToListAsync();

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
        var modules = await _modules.GetAllQueryable()
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
        var personnelCounts = await _personnel.GetAllQueryable()
            .Where(p => p.IsActive)
            .GroupBy(p => p.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var pc in personnelCounts)
        {
            var cm = dto.CustomerModules.FirstOrDefault(c => c.CustomerId == pc.CustomerId);
            if (cm != null) cm.PersonnelCount = pc.Count;
        }

        return dto;
    }

    public async Task<List<ModulePricingDto>> GetModulePricingAsync()
    {
        var pricings = await _pricing.GetAllAsync();
        var pricingMap = pricings.ToDictionary(p => p.ModuleId);

        return SalonPortalModules.All.Select(m =>
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
    }

    public async Task UpdateModulePricingAsync(int moduleId, decimal monthlyPrice)
    {
        var existing = await _pricing.GetByModuleIdAsync(moduleId);
        if (existing != null)
        {
            existing.MonthlyPrice = monthlyPrice;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _pricing.Add(new ModulePricing
            {
                ModuleId = moduleId,
                MonthlyPrice = monthlyPrice
            });
        }

        await _uow.SaveChangesAsync();
    }

    public async Task<int> BulkUpdateModulePricingAsync(List<UpdateModulePricingRequest> prices)
    {
        var existingPricings = await _pricing.GetAllAsync();
        var existingMap = existingPricings.ToDictionary(p => p.ModuleId);

        foreach (var price in prices)
        {
            if (existingMap.TryGetValue(price.ModuleId, out var existing))
            {
                existing.MonthlyPrice = price.MonthlyPrice;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _pricing.Add(new ModulePricing
                {
                    ModuleId = price.ModuleId,
                    MonthlyPrice = price.MonthlyPrice
                });
            }
        }

        await _uow.SaveChangesAsync();
        return prices.Count;
    }

    public async Task<List<ModuleRequestDto>> GetModuleRequestsAsync(bool all)
    {
        var query = _requests.GetAllQueryable()
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
        var pricings = await _pricing.GetAllAsync();
        foreach (var req in requests)
        {
            var module = SalonPortalModules.GetById(req.ModuleId);
            req.ModuleName = module?.SystemName;
            req.ModuleIcon = module?.Icon;
            req.StatusName = ModuleRequestStatuses.GetById(req.StatusId)?.Description;
            req.CatalogPrice = pricings.FirstOrDefault(p => p.ModuleId == req.ModuleId)?.MonthlyPrice;
        }

        return requests;
    }
}

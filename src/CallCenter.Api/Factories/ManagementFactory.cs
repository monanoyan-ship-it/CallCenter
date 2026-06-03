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
    private readonly IModuleRequestEntityService _requests;
    private readonly ISlnSalonProfileEntityService _profiles;
    private readonly ServicePricingFactory _servicePricingFactory;
    private readonly ISupervisorFactory? _supervisorFactory;

    public ManagementFactory(
        ICustomerEntityService customers,
        IUserEntityService users,
        ICustomerProductEntityService products,
        ICustomerPortalModuleEntityService modules,
        ICustomerPersonnelEntityService personnel,
        IModuleRequestEntityService requests,
        ISlnSalonProfileEntityService profiles,
        ServicePricingFactory servicePricingFactory,
        IServiceProvider sp)
    {
        _customers = customers;
        _users = users;
        _products = products;
        _modules = modules;
        _personnel = personnel;
        _requests = requests;
        _profiles = profiles;
        _servicePricingFactory = servicePricingFactory;
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

    public async Task<List<AdminSubMerchantDto>> GetSubMerchantsAsync(int? statusId, string? search)
    {
        var query = from p in _profiles.GetAllQueryable()
                    join c in _customers.GetAllQueryable() on p.CustomerId equals c.Id
                    select new { p, c };

        if (statusId.HasValue)
            query = query.Where(x => x.p.IyzicoOnboardingStatus == statusId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLower();
            query = query.Where(x => x.c.Name.ToLower().Contains(normalized)
                                     || (x.p.Slug != null && x.p.Slug.ToLower().Contains(normalized))
                                     || (x.p.IyzicoSubMerchantKey != null && x.p.IyzicoSubMerchantKey.ToLower().Contains(normalized)));
        }

        var rows = await query
            .OrderBy(x => x.p.IyzicoOnboardingStatus == 1 ? 0 : x.p.IyzicoOnboardingStatus == 3 ? 1 : 2)
            .ThenBy(x => x.c.Name)
            .ToListAsync();

        static string StatusLabel(int status) => status switch
        {
            1 => "Beklemede",
            2 => "Onaylandı",
            3 => "Reddedildi",
            _ => "Başlamadı"
        };

        return rows.Select(x => new AdminSubMerchantDto
        {
            CustomerId = x.c.Id,
            CustomerName = x.c.Name,
            Slug = x.p.Slug,
            IyzicoSubMerchantKey = x.p.IyzicoSubMerchantKey,
            IyzicoSubMerchantType = x.p.IyzicoSubMerchantType,
            ContactName = x.p.IyzicoContactName,
            ContactSurname = x.p.IyzicoContactSurname,
            Iban = x.p.IyzicoIban,
            GsmNumber = null,
            OnboardingStatusId = x.p.IyzicoOnboardingStatus,
            OnboardingStatus = StatusLabel(x.p.IyzicoOnboardingStatus),
            OnboardedAt = x.p.IyzicoOnboardedAt,
            OnboardingError = x.p.IyzicoOnboardingError,
            CommissionPercentOverride = x.c.MarketplaceCommissionPercent != 5m
                ? x.c.MarketplaceCommissionPercent
                : null
        }).ToList();
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
        var prices = await _servicePricingFactory.GetActiveSalonModuleCatalogPricesAsync();
        foreach (var req in requests)
        {
            var module = SalonPortalModules.GetById(req.ModuleId);
            req.ModuleName = module?.SystemName;
            req.ModuleIcon = module?.Icon;
            req.StatusName = ModuleRequestStatuses.GetById(req.StatusId)?.Description;
            req.CatalogPrice = prices.TryGetValue(req.ModuleId, out var price) ? price : null;
        }

        return requests;
    }
}

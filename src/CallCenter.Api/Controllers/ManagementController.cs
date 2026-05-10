using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ManagementController : ControllerBase
{
    private readonly IManagementFactory _factory;

    public ManagementController(IManagementFactory factory)
    {
        _factory = factory;
    }

    [HttpGet("product-types")]
    public IActionResult GetProductTypes()
    {
        var types = ProductTypes.All.Select(t => new { t.Id, t.SystemName, t.Description, t.Icon, t.CssClass }).ToList();
        return Ok(types);
    }

    /// <summary>
    /// PS.12 — Tum salonlarin iyzico Pazaryeri sub-merchant onboarding durumu.
    /// statusId filtresi opsiyonel: 0=NotStarted, 1=Pending, 2=Approved, 3=Rejected.
    /// </summary>
    [HttpGet("sub-merchants")]
    public async Task<ActionResult<List<AdminSubMerchantDto>>> GetSubMerchants(
        [FromQuery] int? statusId,
        [FromQuery] string? search,
        [FromServices] Data.AppDbContext db)
    {
        var query = from p in db.Set<Shared.Entities.SlnSalonProfile>()
                    join c in db.Customers on p.CustomerId equals c.Id
                    select new { p, c };

        if (statusId.HasValue)
            query = query.Where(x => x.p.IyzicoOnboardingStatus == statusId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x => x.c.Name.ToLower().Contains(s)
                                     || (x.p.Slug != null && x.p.Slug.ToLower().Contains(s))
                                     || (x.p.IyzicoSubMerchantKey != null && x.p.IyzicoSubMerchantKey.ToLower().Contains(s)));
        }

        var rows = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            query
                .OrderBy(x => x.p.IyzicoOnboardingStatus == 1 ? 0 : x.p.IyzicoOnboardingStatus == 3 ? 1 : 2)
                .ThenBy(x => x.c.Name));

        static string StatusLabel(int s) => s switch
        {
            1 => "Beklemede",
            2 => "Onaylandi",
            3 => "Reddedildi",
            _ => "Baslamadi"
        };

        var list = rows.Select(x => new AdminSubMerchantDto
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
                : (decimal?)null,
        }).ToList();

        return Ok(list);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ManagementDashboardDto>> GetDashboard()
    {
        return Ok(await _factory.GetDashboardAsync());
    }

    // ═══ SALON ROLE PERMISSIONS ═══

    /// <summary>Rol-sayfa izin matrisi</summary>
    [HttpGet("salon-role-matrix")]
    public async Task<IActionResult> GetSalonRoleMatrix([FromServices] Services.SalonRolePermissionService svc)
    {
        var matrix = await svc.GetMatrixAsync();
        return Ok(matrix);
    }

    /// <summary>Rol-sayfa izin matrisini kaydet</summary>
    [HttpPost("salon-role-matrix")]
    public async Task<IActionResult> SaveSalonRoleMatrix([FromServices] Services.SalonRolePermissionService svc, [FromBody] List<RolePermissionSaveItem> items)
    {
        var perms = items.Select(i => new Shared.Entities.SalonRolePermission
        {
            RoleId = i.RoleId,
            PageName = i.PageName,
            IsAllowed = i.IsAllowed
        }).ToList();

        await svc.SaveMatrixAsync(perms);
        return Ok(new { saved = perms.Count });
    }

    /// <summary>Static yapiyi DB'ye seed et (ilk kullanim)</summary>
    [HttpPost("salon-role-matrix/seed")]
    public async Task<IActionResult> SeedSalonRoleMatrix([FromServices] Services.SalonRolePermissionService svc)
    {
        var count = await svc.SeedFromStaticAsync();
        return Ok(new { seeded = count });
    }

    // ═══ MODULE PRICING ═══

    /// <summary>Tum modul fiyatlarini listele</summary>
    [HttpGet("module-pricing")]
    public async Task<IActionResult> GetModulePricing()
    {
        return Ok(await _factory.GetModulePricingAsync());
    }

    /// <summary>Tek modul fiyat guncelle (upsert)</summary>
    [HttpPost("module-pricing")]
    public async Task<IActionResult> UpdateModulePricing([FromBody] UpdateModulePricingRequest request)
    {
        await _factory.UpdateModulePricingAsync(request.ModuleId, request.MonthlyPrice);
        return Ok(new { success = true });
    }

    /// <summary>Toplu modul fiyat guncelle</summary>
    [HttpPost("module-pricing/bulk")]
    public async Task<IActionResult> BulkUpdateModulePricing([FromBody] BulkUpdateModulePricingRequest request)
    {
        var count = await _factory.BulkUpdateModulePricingAsync(request.Prices);
        return Ok(new { success = true, count });
    }

    // ═══ MODULE REQUESTS (Admin) ═══

    /// <summary>Modul talepleri (varsayilan: sadece bekleyenler, all=true ile tumu)</summary>
    [HttpGet("module-requests")]
    public async Task<IActionResult> GetModuleRequests([FromQuery] bool all = false)
    {
        return Ok(await _factory.GetModuleRequestsAsync(all));
    }
}

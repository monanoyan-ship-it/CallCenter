using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;

namespace CallCenter.Api.Factories;

public class ModuleRequestFactory : IModuleRequestFactory
{
    private readonly IModuleRequestEntityService _requestEs;
    private readonly IModulePricingEntityService _pricingEs;
    private readonly ICustomerPortalModuleEntityService _moduleEs;
    private readonly ISubscriptionFactory _subscriptionFactory;
    private readonly IUnitOfWork _uow;

    public ModuleRequestFactory(
        IModuleRequestEntityService requestEs,
        IModulePricingEntityService pricingEs,
        ICustomerPortalModuleEntityService moduleEs,
        ISubscriptionFactory subscriptionFactory,
        IUnitOfWork uow)
    {
        _requestEs = requestEs;
        _pricingEs = pricingEs;
        _moduleEs = moduleEs;
        _subscriptionFactory = subscriptionFactory;
        _uow = uow;
    }

    public async Task<List<ModuleRequestDto>> GetCustomerRequestsAsync(int customerId)
    {
        var requests = await _requestEs.GetByCustomerAsync(customerId);
        var pricings = await _pricingEs.GetAllAsync();
        return requests.Select(r => MapToDto(r, pricings)).ToList();
    }

    public async Task<List<ModuleRequestDto>> GetPendingRequestsAsync()
    {
        var requests = await _requestEs.GetPendingAsync();
        var pricings = await _pricingEs.GetAllAsync();
        return requests.Select(r => MapToDto(r, pricings)).ToList();
    }

    public async Task<ModuleRequestDto> CreateRequestAsync(int customerId, int personnelId, CreateModuleRequestDto dto)
    {
        var existing = await _moduleEs.GetByCustomerAndModuleAsync(customerId, dto.ModuleId);
        var isDeactivation = dto.RequestTypeId == ModuleRequestTypes.Ids.Deactivation;

        if (isDeactivation)
        {
            // Iptal talebi: modul aktif olmali
            if (existing?.IsActive != true)
                throw new InvalidOperationException("Bu modul zaten aktif degil.");
            // Default modul iptal edilemez
            var moduleDef = SalonPortalModules.GetById(dto.ModuleId);
            if (moduleDef?.IsDefault == true)
                throw new InvalidOperationException("Temel paket modulleri iptal edilemez.");
        }
        else
        {
            // Aktivasyon talebi: modul zaten aktif olmamali
            if (existing?.IsActive == true)
                throw new InvalidOperationException("Bu modul zaten aktif.");
        }

        // Ayni modul icin pending talep var mi kontrol et
        if (await _requestEs.HasPendingRequestAsync(customerId, dto.ModuleId))
            throw new InvalidOperationException("Bu modul icin zaten bekleyen bir talep var.");

        var request = new ModuleRequest
        {
            CustomerId = customerId,
            ModuleId = dto.ModuleId,
            RequestTypeId = dto.RequestTypeId,
            RequestedByPersonnelId = personnelId,
            StatusId = ModuleRequestStatuses.Ids.Pending,
            RequestNotes = dto.Notes
        };

        _requestEs.Add(request);
        await _uow.SaveChangesAsync();

        var pricings = await _pricingEs.GetAllAsync();
        return MapToDto(request, pricings);
    }

    public async Task<ModuleRequestDto> ApproveRequestAsync(int requestId, int reviewerUserId, string? adminNotes)
    {
        var request = await _requestEs.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Talep bulunamadi.");

        if (request.StatusId != ModuleRequestStatuses.Ids.Pending)
            throw new InvalidOperationException("Bu talep zaten degerlendirilmis.");

        request.StatusId = ModuleRequestStatuses.Ids.Approved;
        request.AdminNotes = adminNotes;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedByUserId = reviewerUserId;

        // Talep tipine gore modul aktif/deaktif et
        var module = await _moduleEs.GetByCustomerAndModuleAsync(request.CustomerId, request.ModuleId);

        if (request.RequestTypeId == ModuleRequestTypes.Ids.Deactivation)
        {
            // Iptal talebi onaylandi — modulu deaktif et
            if (module != null)
            {
                module.IsActive = false;
                module.DeactivatedAt = DateTime.UtcNow;
                module.Notes = "Iptal talebi ile deaktif edildi";
            }
        }
        else
        {
            // Aktivasyon talebi onaylandi — modulu aktif et
            if (module != null)
            {
                module.IsActive = true;
                module.DeactivatedAt = null;
                module.ActivatedAt = DateTime.UtcNow;
            }
            else
            {
                _moduleEs.Add(new CustomerPortalModule
                {
                    CustomerId = request.CustomerId,
                    ModuleId = request.ModuleId,
                    IsActive = true,
                    Notes = "Talep ile aktif edildi"
                });
            }
        }

        await _subscriptionFactory.RefreshSubscriptionDisplayMonthlyPriceAsync(request.CustomerId, saveChanges: false);
        await _uow.SaveChangesAsync();

        var pricings = await _pricingEs.GetAllAsync();
        return MapToDto(request, pricings);
    }

    public async Task<ModuleRequestDto> RejectRequestAsync(int requestId, int reviewerUserId, string? adminNotes)
    {
        var request = await _requestEs.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Talep bulunamadi.");

        if (request.StatusId != ModuleRequestStatuses.Ids.Pending)
            throw new InvalidOperationException("Bu talep zaten degerlendirilmis.");

        request.StatusId = ModuleRequestStatuses.Ids.Rejected;
        request.AdminNotes = adminNotes;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedByUserId = reviewerUserId;

        await _uow.SaveChangesAsync();

        var pricings = await _pricingEs.GetAllAsync();
        return MapToDto(request, pricings);
    }

    public async Task CancelRequestAsync(int requestId, int customerId)
    {
        var request = await _requestEs.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Talep bulunamadi.");

        if (request.CustomerId != customerId)
            throw new UnauthorizedAccessException("Bu talep size ait degil.");

        if (request.StatusId != ModuleRequestStatuses.Ids.Pending)
            throw new InvalidOperationException("Sadece bekleyen talepler iptal edilebilir.");

        request.StatusId = ModuleRequestStatuses.Ids.Cancelled;
        await _uow.SaveChangesAsync();
    }

    public async Task<List<ModulePricingDto>> GetAvailableModulesAsync(int customerId)
    {
        var activeIds = await _moduleEs.GetActiveModuleIdsAsync(customerId);
        var pricings = await _pricingEs.GetAllAsync();
        var pricingMap = pricings.ToDictionary(p => p.ModuleId);

        return SalonPortalModules.All
            .Where(m => !m.IsDefault && !activeIds.Contains(m.Id))
            .Select(m =>
            {
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
                    MonthlyPrice = pricingMap.TryGetValue(m.Id, out var p) ? p.MonthlyPrice : 0,
                    HasPricing = pricingMap.ContainsKey(m.Id),
                    IsImplemented = SalonPortalModules.IsImplemented(m.Id)
                };
            })
            .ToList();
    }

    private static ModuleRequestDto MapToDto(ModuleRequest r, List<ModulePricing> pricings)
    {
        var module = SalonPortalModules.GetById(r.ModuleId);
        var pricing = pricings.FirstOrDefault(p => p.ModuleId == r.ModuleId);
        var status = ModuleRequestStatuses.GetById(r.StatusId);
        var requestType = ModuleRequestTypes.GetById(r.RequestTypeId);

        return new ModuleRequestDto
        {
            Id = r.Id,
            Uid = r.Uid,
            CustomerId = r.CustomerId,
            CustomerName = r.Customer?.Name,
            ModuleId = r.ModuleId,
            ModuleName = module?.Description ?? module?.SystemName,
            ModuleIcon = module?.Icon,
            CatalogPrice = pricing?.MonthlyPrice,
            RequestTypeId = r.RequestTypeId,
            RequestTypeName = requestType?.Description,
            StatusId = r.StatusId,
            StatusName = status?.Description,
            RequestNotes = r.RequestNotes,
            AdminNotes = r.AdminNotes,
            RequestedAt = r.RequestedAt,
            ReviewedAt = r.ReviewedAt,
            ReviewedByName = r.ReviewedByUser?.FullName
        };
    }
}

using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnAppointmentFactory : ISlnAppointmentFactory
{
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnServiceEntityService _services;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnNoShowPolicyEntityService _noShowPolicies;
    private readonly ISlnPersonnelSkillEntityService _skills;
    private readonly ISlnServiceComboEntityService _combos;
    private readonly ISlnServiceResourceRequirementEntityService _requirements;
    private readonly ISlnRecipeEntityService _recipes;
    private readonly ISlnProductEntityService _products;
    private readonly ISlnStockMovementEntityService _stockMovements;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly ISlnBranchEntityService _branches;
    private readonly ISlnStockBalanceService _stockBalances;
    private readonly PaymentService _paymentService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnAppointmentFactory> _logger;

    public SlnAppointmentFactory(
        ISlnAppointmentEntityService appointments,
        ISlnServiceEntityService services,
        ISlnClientEntityService clients,
        ISlnNoShowPolicyEntityService noShowPolicies,
        ISlnPersonnelSkillEntityService skills,
        ISlnServiceComboEntityService combos,
        ISlnServiceResourceRequirementEntityService requirements,
        ISlnRecipeEntityService recipes,
        ISlnProductEntityService products,
        ISlnStockMovementEntityService stockMovements,
        ICustomerPersonnelEntityService personnel,
        ISlnBranchEntityService branches,
        ISlnStockBalanceService stockBalances,
        PaymentService paymentService,
        IUnitOfWork uow,
        ILogger<SlnAppointmentFactory> logger)
    {
        _appointments = appointments;
        _services = services;
        _clients = clients;
        _noShowPolicies = noShowPolicies;
        _skills = skills;
        _combos = combos;
        _requirements = requirements;
        _recipes = recipes;
        _products = products;
        _stockMovements = stockMovements;
        _personnel = personnel;
        _branches = branches;
        _stockBalances = stockBalances;
        _paymentService = paymentService;
        _uow = uow;
        _logger = logger;
    }

    private IQueryable<SlnAppointment> IncludeAll(IQueryable<SlnAppointment> q) => q
        .Include(a => a.SlnClient)
        .Include(a => a.Personnel).ThenInclude(p => p!.User)
        .Include(a => a.Branch)
        .Include(a => a.Combo)
        .Include(a => a.Invoices)
        .Include(a => a.Service)
        .Include(a => a.Services).ThenInclude(s => s.SlnService);

    public async Task<List<SlnAppointmentDto>> GetAppointmentsAsync(int customerId, DateTime? from, DateTime? to, int? personnelId = null, int? statusId = null, int? branchId = null, int? slnClientId = null)
    {
        var query = _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId);

        // Musteri detay sayfasi: o musterinin tum randevulari (sube filtresi olmadan — musteri farkli subede de randevu almis olabilir)
        if (slnClientId.HasValue && slnClientId.Value > 0)
            query = query.Where(a => a.SlnClientId == slnClientId.Value);
        else if (branchId.HasValue)
            query = query.Where(a => a.BranchId == branchId.Value);

        if (from.HasValue)
            query = query.Where(a => a.StartTime >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.StartTime <= to.Value);

        if (personnelId.HasValue)
            query = query.Where(a => a.PersonnelId == personnelId.Value);

        if (statusId.HasValue)
            query = query.Where(a => a.StatusId == statusId.Value);

        var appointments = await IncludeAll(query)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        var paidAmounts = await _paymentService.GetAppointmentPaidAmountsAsync(appointments.Select(a => a.Id));

        return appointments
            .Select(a => MapToDto(a, paidAmounts.GetValueOrDefault(a.Id, 0m)))
            .ToList();
    }

    public async Task<SlnAppointmentDto?> GetAppointmentAsync(int appointmentId, int customerId)
    {
        var appointment = await IncludeAll(_appointments.GetAllQueryable())
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CustomerId == customerId);

        if (appointment == null) return null;

        var paidAmount = await _paymentService.GetAppointmentPaidAmountAsync(appointment.Id);
        return MapToDto(appointment, paidAmount);
    }

    public async Task<(SlnAppointmentDto? Appointment, string? Error)> CreateAppointmentAsync(SlnAppointmentCreateDto dto, int userId, int customerId, int? branchId = null)
    {
        var resolved = await ResolveServiceIdsAsync(dto, customerId);
        if (resolved.Error != null)
            return (null, resolved.Error);

        var services = await _services.GetAllQueryable()
            .Where(s => resolved.ServiceIds.Contains(s.Id) && s.CustomerId == customerId && s.IsActive)
            .ToListAsync();

        if (services.Count != resolved.ServiceIds.Count)
            return (null, "Bir veya daha fazla hizmet bulunamadi");

        var personnel = await _personnel.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == dto.PersonnelId && p.CustomerId == customerId && p.IsActive);
        if (personnel == null)
            return (null, "Personel bulunamadi");
        if (branchId.HasValue && personnel.BranchId.HasValue && personnel.BranchId.Value != branchId.Value)
            return (null, "Secilen personel bu sube icin uygun degil");

        var skillScope = await GetSkillScopeAsync(resolved.ServiceIds);
        if (skillScope.HasSkillDefinitions && !skillScope.PersonnelIds.Contains(dto.PersonnelId))
            return (null, "Secilen personel bu hizmetler icin uygun degil");

        var totalMinutes = CalculateBookableMinutes(services);
        var endTime = dto.StartTime.AddMinutes(totalMinutes);

        var client = await _clients.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == dto.SlnClientId && c.CustomerId == customerId);
        if (client == null)
            return (null, "Musteri bulunamadi");
        if (client.IsBlacklisted)
            return (null, $"Bu musteri engellenmis ({client.NoShowCount} kez gelmedi). Engeli kaldirmak icin musteri kartini kullanin.");

        var hasConflict = await CheckConflictAsync(dto.PersonnelId, dto.StartTime, endTime, customerId);
        if (hasConflict)
            return (null, "Secilen saatte personelin baska bir randevusu var");

        // Sube atamasi: JWT branchId > personelin subesi > form/query branch > merkez sube
        var effectiveBranchId = branchId ?? personnel.BranchId ?? dto.BranchId;
        if (effectiveBranchId.HasValue)
        {
            var branchExists = await _branches.GetAllQueryable()
                .AnyAsync(b => b.Id == effectiveBranchId.Value && b.CustomerId == customerId && b.IsActive);
            if (!branchExists) return (null, "Gecersiz sube");
        }
        if (!effectiveBranchId.HasValue)
        {
            var hq = await _branches.GetAllQueryable()
                .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);
            effectiveBranchId = hq?.Id;
        }
        if (effectiveBranchId.HasValue && client.BranchId.HasValue && client.BranchId.Value != effectiveBranchId.Value)
            return (null, "Secilen musteri bu sube icin uygun degil");

        var resourceConflict = await FindResourceConflictAsync(customerId, effectiveBranchId, resolved.ServiceIds, dto.StartTime, endTime);
        if (resourceConflict != null)
            return (null, resourceConflict);

        var appointment = new SlnAppointment
        {
            CustomerId = customerId,
            BranchId = effectiveBranchId,
            SlnClientId = dto.SlnClientId,
            PersonnelId = dto.PersonnelId,
            ComboId = resolved.Combo?.Id,
            StartTime = dto.StartTime,
            EndTime = endTime,
            Notes = dto.Notes,
            CreatedByPersonnelId = userId,
            Services = resolved.ServiceIds.Select((id, i) => new SlnAppointmentService
            {
                SlnServiceId = id,
                SortOrder = i
            }).ToList()
        };

        _appointments.Add(appointment);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni randevu olusturuldu: {AppointmentId} - {StartTime} ({ServiceCount} hizmet)",
            appointment.Id, appointment.StartTime, resolved.ServiceIds.Count);

        var created = await IncludeAll(_appointments.GetAllQueryable())
            .FirstAsync(a => a.Id == appointment.Id);

        return (MapToDto(created), null);
    }

    public async Task<(bool Success, string? Error)> UpdateAppointmentAsync(int appointmentId, SlnAppointmentCreateDto dto, int customerId, int? branchId = null)
    {
        var appointment = await _appointments.GetAllQueryable()
            .Include(a => a.Services)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CustomerId == customerId);

        if (appointment == null) return (false, "Randevu bulunamadi");
        if (appointment.StatusId == 4) return (false, "Iptal edilmis randevu guncellenemez");

        var resolved = await ResolveServiceIdsAsync(dto, customerId);
        if (resolved.Error != null)
            return (false, resolved.Error);

        var services = await _services.GetAllQueryable()
            .Where(s => resolved.ServiceIds.Contains(s.Id) && s.CustomerId == customerId && s.IsActive)
            .ToListAsync();

        if (services.Count != resolved.ServiceIds.Count)
            return (false, "Bir veya daha fazla hizmet bulunamadi");

        var newPersonnel = await _personnel.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == dto.PersonnelId && p.CustomerId == customerId && p.IsActive);
        if (newPersonnel == null)
            return (false, "Personel bulunamadi");
        if (branchId.HasValue && newPersonnel.BranchId.HasValue && newPersonnel.BranchId.Value != branchId.Value)
            return (false, "Secilen personel bu sube icin uygun degil");

        var client = await _clients.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == dto.SlnClientId && c.CustomerId == customerId);
        if (client == null)
            return (false, "Musteri bulunamadi");

        var skillScope = await GetSkillScopeAsync(resolved.ServiceIds);
        if (skillScope.HasSkillDefinitions && !skillScope.PersonnelIds.Contains(dto.PersonnelId))
            return (false, "Secilen personel bu hizmetler icin uygun degil");

        var totalMinutes = CalculateBookableMinutes(services);
        var endTime = dto.StartTime.AddMinutes(totalMinutes);

        var hasConflict = await CheckConflictAsync(dto.PersonnelId, dto.StartTime, endTime, customerId, appointmentId);
        if (hasConflict) return (false, "Secilen saatte personelin baska bir randevusu var");

        var effectiveBranchId = branchId ?? newPersonnel.BranchId ?? dto.BranchId ?? appointment.BranchId;
        if (effectiveBranchId.HasValue)
        {
            var branchExists = await _branches.GetAllQueryable()
                .AnyAsync(b => b.Id == effectiveBranchId.Value && b.CustomerId == customerId && b.IsActive);
            if (!branchExists) return (false, "Gecersiz sube");
        }
        if (!effectiveBranchId.HasValue)
        {
            var hq = await _branches.GetAllQueryable()
                .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);
            effectiveBranchId = hq?.Id;
        }
        if (effectiveBranchId.HasValue && client.BranchId.HasValue && client.BranchId.Value != effectiveBranchId.Value)
            return (false, "Secilen musteri bu sube icin uygun degil");

        var resourceConflict = await FindResourceConflictAsync(customerId, effectiveBranchId, resolved.ServiceIds, dto.StartTime, endTime, appointmentId);
        if (resourceConflict != null) return (false, resourceConflict);

        appointment.SlnClientId = dto.SlnClientId;
        appointment.BranchId = effectiveBranchId;
        appointment.PersonnelId = dto.PersonnelId;
        appointment.ComboId = resolved.Combo?.Id;
        appointment.ServiceId = null;
        appointment.StartTime = dto.StartTime;
        appointment.EndTime = endTime;
        appointment.Notes = dto.Notes;
        appointment.UpdatedAt = DateTime.UtcNow;

        // Eski hizmetleri temizle, yenilerini ekle
        appointment.Services.Clear();
        foreach (var (id, i) in resolved.ServiceIds.Select((id, i) => (id, i)))
            appointment.Services.Add(new SlnAppointmentService { SlnServiceId = id, SortOrder = i });

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error, decimal Penalty)> UpdateStatusAsync(int appointmentId, int statusId, int customerId)
    {
        var appointment = await IncludeAll(_appointments.GetAllQueryable())
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CustomerId == customerId);

        if (appointment == null) return (false, "Randevu bulunamadi", 0);

        var policy = await _noShowPolicies.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.CustomerId == customerId && p.IsActive);

        decimal penalty = 0;

        // ═══ GELMEDİ (StatusId=5) ═══
        if (statusId == 5 && appointment.SlnClient != null)
        {
            appointment.SlnClient.NoShowCount++;

            if (policy != null)
            {
                // Ceza hesapla
                penalty = policy.NoShowFee > 0 ? policy.NoShowFee : appointment.DepositAmount;
                appointment.PenaltyAmount = penalty;

                // Engelleme esigi kontrolu
                if (appointment.SlnClient.NoShowCount >= policy.BlacklistThreshold)
                {
                    appointment.SlnClient.IsBlacklisted = true;
                    _logger.LogWarning("Musteri engellendi: ClientId={ClientId}, NoShowCount={Count}",
                        appointment.SlnClientId, appointment.SlnClient.NoShowCount);
                }
            }

            _logger.LogInformation("Randevu gelmedi: AppointmentId={Id}, ClientId={ClientId}, NoShowCount={Count}",
                appointmentId, appointment.SlnClientId, appointment.SlnClient.NoShowCount);
        }

        // ═══ İPTAL (StatusId=4) ═══
        if (statusId == 4 && policy != null)
        {
            var hoursUntilAppointment = (appointment.StartTime - DateTime.UtcNow).TotalHours;

            if (hoursUntilAppointment < policy.FreeCancellationHours)
            {
                // Gec iptal — ceza uygula
                penalty = policy.LateCancellationFee > 0 ? policy.LateCancellationFee : appointment.DepositAmount;
                appointment.PenaltyAmount = penalty;
                appointment.DepositRefunded = false;
            }
            else
            {
                // Ucretsiz iptal — depozito iade
                appointment.PenaltyAmount = 0;
                if (appointment.DepositAmount > 0)
                    appointment.DepositRefunded = true;
            }
        }

        if (statusId == 3)
        {
            var (stockOk, stockError) = await ConsumeRecipeStockForCompletedAppointmentAsync(appointment);
            if (!stockOk) return (false, stockError, penalty);
        }

        appointment.StatusId = statusId;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null, penalty);
    }

    private async Task<(bool Success, string? Error)> ConsumeRecipeStockForCompletedAppointmentAsync(SlnAppointment appointment)
    {
        var movementNotePrefix = $"Randevu:{appointment.Id}";
        var alreadyConsumed = await _stockMovements.GetAllQueryable()
            .AnyAsync(m => m.CustomerId == appointment.CustomerId
                        && m.MovementTypeId == 3
                        && m.Notes != null
                        && m.Notes.StartsWith(movementNotePrefix));
        if (alreadyConsumed) return (true, null);

        var serviceIds = appointment.Services?
            .Select(s => s.SlnServiceId)
            .Where(id => id > 0)
            .Distinct()
            .ToList() ?? new List<int>();

        if (serviceIds.Count == 0 && appointment.ServiceId.HasValue)
            serviceIds.Add(appointment.ServiceId.Value);

        if (serviceIds.Count == 0) return (true, null);

        var recipes = await _recipes.GetAllQueryable()
            .Include(r => r.Items)
            .Where(r => r.CustomerId == appointment.CustomerId
                     && r.IsActive
                     && r.ServiceId.HasValue
                     && serviceIds.Contains(r.ServiceId.Value))
            .ToListAsync();

        var recipeItems = recipes
            .SelectMany(r => r.Items.Select(i => new { Recipe = r, Item = i }))
            .ToList();
        if (recipeItems.Count == 0) return (true, null);

        var productIds = recipeItems.Select(x => x.Item.ProductId).Distinct().ToList();
        var products = await _products.GetAllQueryable()
            .Where(p => p.CustomerId == appointment.CustomerId
                     && productIds.Contains(p.Id)
                     && (p.BranchId == null || p.BranchId == appointment.BranchId))
            .ToDictionaryAsync(p => p.Id);

        foreach (var productGroup in recipeItems.GroupBy(x => x.Item.ProductId))
        {
            if (!products.TryGetValue(productGroup.Key, out var product))
                return (false, "Recete urunu bulunamadi");

            var totalQuantity = productGroup.Sum(x => x.Item.Quantity);
            var availableStock = await _stockBalances.GetStockQuantityAsync(appointment.CustomerId, product.Id, appointment.BranchId, product.StockQuantity);
            if (availableStock < totalQuantity)
                return (false, $"Yetersiz stok: {product.Name} (Mevcut: {availableStock:0.##} {product.Unit})");
        }

        foreach (var entry in recipeItems)
        {
            var product = products[entry.Item.ProductId];
            var (stockOk, stockError) = await _stockBalances.AdjustStockAsync(
                product, appointment.CustomerId, appointment.BranchId, -entry.Item.Quantity, preventNegative: true);
            if (!stockOk) return (false, stockError);
            await _stockBalances.SyncProductTotalAsync(product, appointment.CustomerId);
            _stockMovements.Add(new SlnStockMovement
            {
                CustomerId = appointment.CustomerId,
                BranchId = appointment.BranchId,
                ProductId = product.Id,
                MovementTypeId = 3,
                Quantity = entry.Item.Quantity,
                UnitPrice = product.PurchasePrice,
                Notes = $"{movementNotePrefix} | Recete:{entry.Recipe.Name}",
                CreatedByPersonnelId = appointment.CreatedByPersonnelId
            });
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAppointmentAsync(int appointmentId, int customerId)
    {
        var appointment = await _appointments.GetAllQueryable()
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CustomerId == customerId);

        if (appointment == null) return (false, "Randevu bulunamadi");

        _appointments.Remove(appointment);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> CheckConflictAsync(int personnelId, DateTime startTime, DateTime endTime, int customerId, int? excludeAppointmentId = null)
    {
        var query = _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId
                && a.PersonnelId == personnelId
                && a.StatusId != 4 // Iptal edilmis randevular haric
                && a.StatusId != 5 // NoShow haric
                && a.StartTime < endTime
                && a.EndTime > startTime);

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        return await query.AnyAsync();
    }

    private async Task<AppointmentServiceResolution> ResolveServiceIdsAsync(SlnAppointmentCreateDto dto, int customerId)
    {
        var serviceIds = dto.ServiceIds.Where(id => id > 0).Distinct().ToList();
        SlnServiceCombo? combo = null;

        if (dto.ComboId.HasValue && dto.ComboId.Value > 0)
        {
            combo = await _combos.GetAllQueryable()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == dto.ComboId.Value && c.CustomerId == customerId && c.IsActive);

            if (combo == null)
                return new([], null, "Combo bulunamadi");

            var comboServiceIds = combo.Items.OrderBy(i => i.SortOrder).Select(i => i.ServiceId).ToList();
            if (comboServiceIds.Count == 0)
                return new([], combo, "Combo icinde hizmet yok");

            serviceIds = comboServiceIds
                .Concat(serviceIds.Where(id => !comboServiceIds.Contains(id)))
                .Distinct()
                .ToList();
        }

        if (serviceIds.Count == 0)
            return new([], combo, "En az bir hizmet secilmeli");

        return new(serviceIds, combo, null);
    }

    private async Task<string?> FindResourceConflictAsync(int customerId, int? branchId, List<int> serviceIds, DateTime startTime, DateTime endTime, int? excludeAppointmentId = null)
    {
        if (serviceIds.Count == 0) return null;

        var requirements = await _requirements.GetAllQueryable()
            .Where(r => serviceIds.Contains(r.ServiceId))
            .Include(r => r.Resource)
            .ToListAsync();

        var needed = requirements
            .Where(r => r.Resource != null)
            .GroupBy(r => r.ResourceId)
            .Select(g => new
            {
                Resource = g.First().Resource!,
                QuantityRequired = g.Max(x => Math.Max(1, x.QuantityRequired))
            })
            .ToList();

        if (needed.Count == 0) return null;

        var overlappingQuery = _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId
                && a.StatusId != 4
                && a.StatusId != 5
                && a.StartTime < endTime
                && a.EndTime > startTime);

        if (excludeAppointmentId.HasValue)
            overlappingQuery = overlappingQuery.Where(a => a.Id != excludeAppointmentId.Value);

        var overlapping = await overlappingQuery
            .Include(a => a.Service)!.ThenInclude(s => s!.ResourceRequirements)
            .Include(a => a.Services).ThenInclude(s => s.SlnService)!.ThenInclude(s => s!.ResourceRequirements)
            .ToListAsync();

        foreach (var item in needed)
        {
            var resource = item.Resource;
            if (resource.CustomerId != customerId || !resource.IsActive)
                return $"{resource.Name} kaynagi aktif degil";
            if (resource.BranchId.HasValue && branchId.HasValue && resource.BranchId.Value != branchId.Value)
                return $"{resource.Name} bu sube icin uygun degil";
            if (resource.BranchId.HasValue && !branchId.HasValue)
                return $"{resource.Name} icin sube secimi gerekli";

            var used = 0;
            foreach (var appointment in overlapping)
            {
                var appointmentRequirements = appointment.Services.Count > 0
                    ? appointment.Services
                        .Where(s => s.SlnService != null)
                        .SelectMany(s => s.SlnService!.ResourceRequirements)
                    : appointment.Service?.ResourceRequirements ?? [];

                used += appointmentRequirements
                    .Where(r => r.ResourceId == resource.Id)
                    .Select(r => Math.Max(1, r.QuantityRequired))
                    .DefaultIfEmpty(0)
                    .Max();
            }

            if (used + item.QuantityRequired > resource.Quantity)
                return $"{resource.Name} kapasitesi dolu";
        }

        return null;
    }

    private static int CalculateBookableMinutes(IEnumerable<SlnService> services)
        => services.Sum(s => Math.Max(5, Math.Max(s.DurationMinutes, s.ProcessingMinutes) + s.BufferBeforeMinutes + s.BufferAfterMinutes));

    private static SlnAppointmentDto MapToDto(SlnAppointment a, decimal paidAmount = 0m)
    {
        // Yeni kayitlar Services koleksiyonunu kullanir, eski kayitlar ServiceId FK'yi
        var serviceIds = a.Services.Count > 0
            ? a.Services.OrderBy(s => s.SortOrder).Select(s => s.SlnServiceId).ToList()
            : a.ServiceId.HasValue ? new List<int> { a.ServiceId.Value } : new List<int>();

        var serviceNames = a.Services.Count > 0
            ? a.Services.OrderBy(s => s.SortOrder).Select(s => s.SlnService?.Name ?? "").ToList()
            : a.Service != null ? new List<string> { a.Service.Name } : new List<string>();

        var duration = (int)(a.EndTime - a.StartTime).TotalMinutes;
        var invoice = a.Invoices
            .Where(i => i.StatusId != 3)
            .OrderByDescending(i => i.InvoiceDate)
            .FirstOrDefault()
            ?? a.Invoices.OrderByDescending(i => i.InvoiceDate).FirstOrDefault();

        return new SlnAppointmentDto
        {
            Id = a.Id,
            SlnClientId = a.SlnClientId,
            ClientName = a.SlnClient?.FullName ?? "",
            ClientPhone = a.SlnClient?.Phone,
            PersonnelId = a.PersonnelId,
            PersonnelName = a.Personnel?.User?.FullName ?? "",
            BranchId = a.BranchId,
            BranchName = a.Branch?.Name,
            ComboId = a.ComboId,
            ComboName = a.Combo?.Name,
            ServiceIds = serviceIds,
            ServiceNames = serviceNames,
            DurationMinutes = duration,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            StatusId = a.StatusId,
            Notes = a.Notes,
            IsPrepaid = a.IsPrepaid,
            PrepaidAmount = a.PrepaidAmount,
            PaidAmount = paidAmount,
            DepositAmount = a.DepositAmount,
            ClientNoShowCount = a.SlnClient?.NoShowCount ?? 0,
            ClientIsBlacklisted = a.SlnClient?.IsBlacklisted ?? false,
            InvoiceId = invoice?.Id,
            InvoiceNo = invoice?.InvoiceNo
        };
    }

    private sealed record AppointmentServiceResolution(List<int> ServiceIds, SlnServiceCombo? Combo, string? Error);

    private async Task<(List<int> PersonnelIds, bool HasSkillDefinitions)> GetSkillScopeAsync(List<int> serviceIds)
    {
        var skillRows = await _skills.GetAllQueryable()
            .Where(s => serviceIds.Contains(s.ServiceId))
            .Select(s => new { s.ServiceId, s.PersonnelId })
            .ToListAsync();

        var serviceIdsWithSkills = skillRows.Select(s => s.ServiceId).Distinct().ToList();
        var personnelIds = skillRows
            .GroupBy(s => s.PersonnelId)
            .Where(g => serviceIdsWithSkills.All(serviceId => g.Any(s => s.ServiceId == serviceId)))
            .Select(g => g.Key)
            .ToList();

        return (personnelIds, serviceIdsWithSkills.Count > 0);
    }

    public async Task<List<object>> GetAvailableStaffAsync(int customerId, List<int> serviceIds, int? branchId = null)
    {
        // Skill eslemesi olan personelleri bul
        var skillRows = await _skills.GetAllQueryable()
            .Where(s => serviceIds.Contains(s.ServiceId))
            .Select(s => new { s.ServiceId, s.PersonnelId })
            .ToListAsync();

        var serviceIdsWithSkills = skillRows.Select(s => s.ServiceId).Distinct().ToList();
        var skilledPersonnelIds = skillRows
            .GroupBy(s => s.PersonnelId)
            .Where(g => serviceIdsWithSkills.All(serviceId => g.Any(s => s.ServiceId == serviceId)))
            .Select(g => g.Key)
            .ToList();

        var personnelQuery = _personnel.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && p.IsActive);

        if (branchId.HasValue)
            personnelQuery = personnelQuery.Where(p => p.BranchId == branchId.Value || p.BranchId == null);

        // Skill tanimlanmissa filtrele, tanimlanmamissa tum aktif personelleri don
        if (serviceIdsWithSkills.Count > 0)
            personnelQuery = personnelQuery.Where(p => skilledPersonnelIds.Contains(p.Id));

        return await personnelQuery
            .Include(p => p.User)
            .OrderBy(p => p.User.FullName)
            .Select(p => (object)new
            {
                p.Id,
                Name = p.User.FullName,
                p.Title,
                p.PhotoUrl,
                p.Specialty,
                p.BranchId
            })
            .ToListAsync();
    }

    public async Task<List<object>> GetAvailableSlotsAsync(int customerId, int personnelId, DateTime date, int durationMinutes, int? branchId = null, List<int>? serviceIds = null)
    {
        if (personnelId <= 0 || durationMinutes <= 0)
            return [];

        var requestedServiceIds = (serviceIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (requestedServiceIds.Count > 0)
        {
            var validServiceCount = await _services.GetAllQueryable()
                .CountAsync(s => requestedServiceIds.Contains(s.Id) && s.CustomerId == customerId && s.IsActive);
            if (validServiceCount != requestedServiceIds.Count)
                return [];
        }

        // Personel kendi calisma saati varsa onu kullan, yoksa subeye dus.
        var personnel = await _personnel.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == personnelId && p.CustomerId == customerId && p.IsActive);
        if (personnel == null)
            return [];

        if (branchId.HasValue && personnel.BranchId.HasValue && personnel.BranchId.Value != branchId.Value)
            return [];

        if (requestedServiceIds.Count > 0)
        {
            var skillScope = await GetSkillScopeAsync(requestedServiceIds);
            if (skillScope.HasSkillDefinitions && !skillScope.PersonnelIds.Contains(personnelId))
                return [];
        }

        var effectiveBranchId = branchId ?? personnel.BranchId;
        SlnBranch? branch = null;

        if (effectiveBranchId.HasValue)
        {
            branch = await _branches.GetAllQueryable()
                .FirstOrDefaultAsync(b => b.Id == effectiveBranchId.Value && b.CustomerId == customerId && b.IsActive);
            if (branch == null)
                return [];
        }
        else
        {
            branch = await _branches.GetAllQueryable()
                .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter && b.IsActive);
            effectiveBranchId = branch?.Id;
        }

        var dayKey = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "mon", DayOfWeek.Tuesday => "tue", DayOfWeek.Wednesday => "wed",
            DayOfWeek.Thursday => "thu", DayOfWeek.Friday => "fri", DayOfWeek.Saturday => "sat",
            _ => "sun"
        };

        var openHour = 9; var openMin = 0;
        var closeHour = 19; var closeMin = 0;

        var hoursJson = !string.IsNullOrWhiteSpace(personnel?.WorkingHoursJson)
            ? personnel!.WorkingHoursJson
            : branch?.WorkingHoursJson;

        if (hoursJson != null)
        {
            try
            {
                var hours = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(hoursJson);
                if (hours != null && hours.TryGetValue(dayKey, out var val))
                {
                    if (val == "closed") return new List<object>();
                    var parts = val.Split('-');
                    if (parts.Length == 2)
                    {
                        var openParts = parts[0].Split(':');
                        var closeParts = parts[1].Split(':');
                        openHour = int.Parse(openParts[0]);
                        openMin = openParts.Length > 1 ? int.Parse(openParts[1]) : 0;
                        closeHour = int.Parse(closeParts[0]);
                        closeMin = closeParts.Length > 1 ? int.Parse(closeParts[1]) : 0;
                    }
                }
            }
            catch { }
        }

        // Personelin o gundeki mevcut randevulari (UTC)
        var dayStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);
        var existingAppointments = await _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId
                && a.PersonnelId == personnelId
                && a.StatusId != 4 && a.StatusId != 5
                && a.StartTime >= dayStart && a.StartTime < dayEnd)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        // Musait slotlari hesapla (30 dk aralikla)
        var slots = new List<object>();
        var slotStart = dayStart.AddHours(openHour).AddMinutes(openMin);
        var dayClose = dayStart.AddHours(closeHour).AddMinutes(closeMin);

        while (slotStart.AddMinutes(durationMinutes) <= dayClose)
        {
            var slotEnd = slotStart.AddMinutes(durationMinutes);
            var hasConflict = existingAppointments.Any(a => slotStart < a.EndTime && slotEnd > a.StartTime);
            var resourceConflict = requestedServiceIds.Count > 0
                ? await FindResourceConflictAsync(customerId, effectiveBranchId, requestedServiceIds, slotStart, slotEnd)
                : null;

            slots.Add(new
            {
                startTime = slotStart,
                endTime = slotEnd,
                available = !hasConflict && resourceConflict == null,
                resourceConflict,
                timeText = slotStart.ToString("HH:mm")
            });

            slotStart = slotStart.AddMinutes(30);
        }

        return slots;
    }

    public async Task<object> NormalizeBranchesAsync(int customerId)
    {
        var hqBranch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);

        if (hqBranch == null)
            return new { updated = 0, error = "Merkez sube bulunamadi. Once bir subeyi merkez olarak isaretleyin." };

        // TUM aktif randevulari getir (orphan + yanlis atanmis dahil)
        var appointments = await _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId && a.StatusId != 4) // iptal haric
            .Include(a => a.Personnel)
            .ToListAsync();

        int syncedFromPersonnel = 0, assignedToHq = 0, alreadyCorrect = 0;
        foreach (var a in appointments)
        {
            var personnelBranchId = a.Personnel?.BranchId;
            int? desiredBranchId = personnelBranchId ?? (a.BranchId == null ? hqBranch.Id : a.BranchId);

            if (a.BranchId == desiredBranchId) { alreadyCorrect++; continue; }

            a.BranchId = desiredBranchId;
            if (personnelBranchId.HasValue) syncedFromPersonnel++;
            else assignedToHq++;
        }

        await _uow.SaveChangesAsync();
        var updated = syncedFromPersonnel + assignedToHq;
        _logger.LogInformation("NormalizeBranches: {Updated} guncellendi (personelSenkron={P}, merkezAta={H}, dokunulmadi={S}, CustomerId={C})",
            updated, syncedFromPersonnel, assignedToHq, alreadyCorrect, customerId);

        return new
        {
            updated,
            syncedFromPersonnel,
            assignedToHq,
            alreadyCorrect,
            hqBranchId = hqBranch.Id,
            hqBranchName = hqBranch.Name
        };
    }
}

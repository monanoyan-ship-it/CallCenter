using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnServiceSessionFactory : ISlnServiceSessionFactory
{
    private readonly ISlnServiceSessionPlanEntityService _plans;
    private readonly ISlnServiceSessionRecordEntityService _records;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnServiceEntityService _services;
    private readonly IUnitOfWork _uow;

    public SlnServiceSessionFactory(
        ISlnServiceSessionPlanEntityService plans,
        ISlnServiceSessionRecordEntityService records,
        ISlnClientEntityService clients,
        ISlnServiceEntityService services,
        IUnitOfWork uow)
    {
        _plans = plans;
        _records = records;
        _clients = clients;
        _services = services;
        _uow = uow;
    }

    public async Task<List<SlnServiceSessionPlanDto>> GetPlansAsync(int customerId, int? clientId = null, int? branchId = null, bool activeOnly = false)
    {
        var query = SalonBranchScope.ApplyToServiceSessionPlans(
                _plans.GetAllQueryable().Where(p => p.CustomerId == customerId),
                branchId)
            .Include(p => p.Service)
            .Include(p => p.SlnClient)
            .Include(p => p.Records).ThenInclude(r => r.Service)
            .Include(p => p.Records).ThenInclude(r => r.Personnel).ThenInclude(p => p!.User)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(p => p.SlnClientId == clientId.Value);

        if (activeOnly)
            query = query.Where(p => p.IsActive && p.RemainingSessions > 0);

        var plans = await query
            .OrderByDescending(p => p.SoldAt)
            .ToListAsync();

        return plans.Select(MapPlan).ToList();
    }

    public async Task<List<SlnServiceSessionPlanDto>> CreatePlansFromInvoiceAsync(
        int customerId,
        int slnClientId,
        int invoiceId,
        IEnumerable<SlnServiceSessionPlanSaleLine> lines,
        int userId,
        int? branchId = null)
    {
        if (slnClientId <= 0)
            return [];

        var clientExists = await SalonBranchScope.ApplyToClients(
                _clients.GetAllQueryable().Where(c => c.Id == slnClientId && c.CustomerId == customerId),
                branchId)
            .AnyAsync();
        if (!clientExists)
            return [];

        var saleLines = lines
            .Where(l => l.ServiceId > 0 && l.Quantity > 0)
            .ToList();
        if (saleLines.Count == 0)
            return [];

        var serviceIds = saleLines.Select(l => l.ServiceId).Distinct().ToList();
        var services = await _services.GetAllQueryable()
            .Where(s => s.CustomerId == customerId
                && s.IsActive
                && s.SessionCount > 1
                && serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        if (services.Count == 0)
            return [];

        var created = new List<SlnServiceSessionPlan>();
        foreach (var line in saleLines)
        {
            if (!services.TryGetValue(line.ServiceId, out var service))
                continue;

            var totalSessions = service.SessionCount * Math.Max(1, line.Quantity);
            var plan = new SlnServiceSessionPlan
            {
                CustomerId = customerId,
                SlnClientId = slnClientId,
                BranchId = branchId,
                ServiceId = service.Id,
                SourceInvoiceId = invoiceId > 0 ? invoiceId : null,
                SourceInvoiceItemId = line.InvoiceItemId,
                TotalSessions = totalSessions,
                UsedSessions = 0,
                RemainingSessions = totalSessions,
                SaleAmount = line.SaleAmount,
                PaidAmount = Math.Min(line.PaidAmount, line.SaleAmount),
                SoldByPersonnelId = userId > 0 ? userId : null,
                SoldAt = DateTime.UtcNow,
                IsActive = true
            };

            _plans.Add(plan);
            created.Add(plan);
        }

        if (created.Count == 0)
            return [];

        await _uow.SaveChangesAsync();

        var ids = created.Select(p => p.Id).ToList();
        return (await GetPlansAsync(customerId, slnClientId, branchId))
            .Where(p => ids.Contains(p.Id))
            .ToList();
    }

    public async Task<(SlnServiceSessionRecordDto? Record, string? Error)> RecordSessionAsync(
        SlnServiceSessionUseDto dto,
        int userId,
        int customerId,
        int? branchId = null)
    {
        var plan = await SalonBranchScope.ApplyToServiceSessionPlans(
                _plans.GetAllQueryable().Where(p => p.Id == dto.PlanId && p.CustomerId == customerId),
                branchId)
            .Include(p => p.Records)
            .Include(p => p.Service)
            .FirstOrDefaultAsync();

        if (plan == null) return (null, "Seans plani bulunamadi");
        if (!plan.IsActive) return (null, "Seans plani aktif degil");
        if (plan.RemainingSessions <= 0) return (null, "Kalan seans yok");
        if (plan.ExpiresAt.HasValue && plan.ExpiresAt.Value < DateTime.UtcNow) return (null, "Seans planinin suresi dolmus");

        var nextNumber = plan.UsedSessions + 1;
        var record = new SlnServiceSessionRecord
        {
            PlanId = plan.Id,
            CustomerId = plan.CustomerId,
            SlnClientId = plan.SlnClientId,
            BranchId = plan.BranchId,
            ServiceId = plan.ServiceId,
            SessionNumber = nextNumber,
            PerformedAt = dto.PerformedAt ?? DateTime.UtcNow,
            PersonnelId = dto.PersonnelId ?? (userId > 0 ? userId : null),
            InvoiceId = dto.InvoiceId,
            InvoiceItemId = dto.InvoiceItemId,
            SlnAppointmentId = dto.SlnAppointmentId,
            TreatmentRecordId = dto.TreatmentRecordId,
            Notes = dto.Notes,
            CreatedByPersonnelId = userId > 0 ? userId : null
        };

        plan.UsedSessions++;
        plan.RemainingSessions--;
        if (plan.RemainingSessions == 0)
        {
            plan.IsActive = false;
            plan.CompletedAt = record.PerformedAt;
        }

        _records.Add(record);
        await _uow.SaveChangesAsync();

        var mapped = await _records.GetAllQueryable()
            .Include(r => r.Service)
            .Include(r => r.Personnel).ThenInclude(p => p!.User)
            .FirstAsync(r => r.Id == record.Id);

        return (MapRecord(mapped), null);
    }

    private static SlnServiceSessionPlanDto MapPlan(SlnServiceSessionPlan p) => new()
    {
        Id = p.Id,
        SlnClientId = p.SlnClientId,
        ClientName = p.SlnClient?.FullName,
        ServiceId = p.ServiceId,
        ServiceName = p.Service?.Name ?? "",
        BranchId = p.BranchId,
        SourceInvoiceId = p.SourceInvoiceId,
        SourceInvoiceItemId = p.SourceInvoiceItemId,
        TotalSessions = p.TotalSessions,
        UsedSessions = p.UsedSessions,
        RemainingSessions = p.RemainingSessions,
        SaleAmount = p.SaleAmount,
        PaidAmount = p.PaidAmount,
        IsActive = p.IsActive,
        SoldAt = p.SoldAt,
        CompletedAt = p.CompletedAt,
        ExpiresAt = p.ExpiresAt,
        Records = p.Records.OrderBy(r => r.SessionNumber).Select(MapRecord).ToList()
    };

    private static SlnServiceSessionRecordDto MapRecord(SlnServiceSessionRecord r) => new()
    {
        Id = r.Id,
        PlanId = r.PlanId,
        SlnClientId = r.SlnClientId,
        ServiceId = r.ServiceId,
        ServiceName = r.Service?.Name ?? "",
        SessionNumber = r.SessionNumber,
        PerformedAt = r.PerformedAt,
        PersonnelId = r.PersonnelId,
        PersonnelName = r.Personnel?.User?.FullName ?? r.Personnel?.Title,
        InvoiceId = r.InvoiceId,
        InvoiceItemId = r.InvoiceItemId,
        SlnAppointmentId = r.SlnAppointmentId,
        TreatmentRecordId = r.TreatmentRecordId,
        Notes = r.Notes
    };
}

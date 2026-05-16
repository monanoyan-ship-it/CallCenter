using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnPackageFactory : ISlnPackageFactory
{
    private readonly ISlnPackageDefinitionEntityService _defEs;
    private readonly ISlnClientPackageEntityService _pkgEs;
    private readonly ISlnPackageUsageEntityService _usageEs;
    private readonly ISlnClientEntityService _clients;
    private readonly IUnitOfWork _uow;

    public SlnPackageFactory(
        ISlnPackageDefinitionEntityService defEs,
        ISlnClientPackageEntityService pkgEs,
        ISlnPackageUsageEntityService usageEs,
        ISlnClientEntityService clients,
        IUnitOfWork uow)
    {
        _defEs = defEs;
        _pkgEs = pkgEs;
        _usageEs = usageEs;
        _clients = clients;
        _uow = uow;
    }

    // ═══ Paket Tanimlari ═══

    public async Task<List<SlnPackageDefinitionDto>> GetDefinitionsAsync(int customerId)
    {
        return await _defEs.GetAllQueryable()
            .Where(d => d.CustomerId == customerId)
            .Include(d => d.Service)
            .OrderBy(d => d.Name)
            .Select(d => new SlnPackageDefinitionDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                ServiceId = d.ServiceId,
                ServiceName = d.Service != null ? d.Service.Name : "",
                TotalSessions = d.TotalSessions,
                Price = d.Price,
                PricePerSession = d.TotalSessions > 0 ? Math.Round(d.Price / d.TotalSessions, 2) : 0,
                ValidDays = d.ValidDays,
                IsActive = d.IsActive
            }).ToListAsync();
    }

    public async Task<SlnPackageDefinitionDto> CreateDefinitionAsync(SlnPackageDefinitionCreateDto dto, int customerId)
    {
        var existing = await _defEs.GetAllQueryable()
            .FirstOrDefaultAsync(d => d.CustomerId == customerId && d.ServiceId == dto.ServiceId);
        if (existing != null)
        {
            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.TotalSessions = dto.TotalSessions;
            existing.Price = dto.Price;
            existing.ValidDays = dto.ValidDays;
            existing.IsActive = dto.IsActive;
            await _uow.SaveChangesAsync();
            return (await GetDefinitionsAsync(customerId)).First(d => d.Id == existing.Id);
        }

        var def = new SlnPackageDefinition
        {
            CustomerId = customerId,
            Name = dto.Name,
            Description = dto.Description,
            ServiceId = dto.ServiceId,
            TotalSessions = dto.TotalSessions,
            Price = dto.Price,
            ValidDays = dto.ValidDays,
            IsActive = dto.IsActive
        };
        _defEs.Add(def);
        await _uow.SaveChangesAsync();

        return (await GetDefinitionsAsync(customerId)).First(d => d.Id == def.Id);
    }

    public async Task<(bool Success, string? Error)> UpdateDefinitionAsync(int id, SlnPackageDefinitionCreateDto dto, int customerId)
    {
        var def = await _defEs.GetAllQueryable().FirstOrDefaultAsync(d => d.Id == id && d.CustomerId == customerId);
        if (def == null) return (false, "Paket tanimi bulunamadi");

        var duplicateExists = await _defEs.GetAllQueryable()
            .AnyAsync(d => d.Id != id && d.CustomerId == customerId && d.ServiceId == dto.ServiceId);
        if (duplicateExists) return (false, "Bu hizmet icin zaten seans tanimi var");

        def.Name = dto.Name;
        def.Description = dto.Description;
        def.ServiceId = dto.ServiceId;
        def.TotalSessions = dto.TotalSessions;
        def.Price = dto.Price;
        def.ValidDays = dto.ValidDays;
        def.IsActive = dto.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteDefinitionAsync(int id, int customerId)
    {
        var def = await _defEs.GetAllQueryable().FirstOrDefaultAsync(d => d.Id == id && d.CustomerId == customerId);
        if (def == null) return (false, "Paket tanimi bulunamadi");
        var hasSoldPlans = await _pkgEs.GetAllQueryable()
            .AnyAsync(p => p.CustomerId == customerId && p.PackageDefinitionId == id);
        if (hasSoldPlans) return (false, "Satilmis seans kaydi olan tanim silinemez. Pasife alin veya once satis kayitlarini kapatin.");
        _defEs.Remove(def);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ Musteri Paketleri ═══

    public async Task<List<SlnClientPackageDto>> GetClientPackagesAsync(int customerId, int? clientId = null, int? branchId = null)
    {
        var query = SalonBranchScope.ApplyToClientPackages(
                _pkgEs.GetAllQueryable().Where(p => p.CustomerId == customerId),
                branchId)
            .Include(p => p.PackageDefinition).ThenInclude(d => d!.Service)
            .Include(p => p.SlnClient)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(p => p.SlnClientId == clientId.Value);

        return await query.OrderByDescending(p => p.CreatedAt).Select(p => new SlnClientPackageDto
        {
            Id = p.Id,
            PackageDefinitionId = p.PackageDefinitionId,
            ServiceId = p.PackageDefinition != null ? p.PackageDefinition.ServiceId : 0,
            BranchId = p.BranchId,
            SourceInvoiceId = p.SourceInvoiceId,
            SourceInvoiceItemId = p.SourceInvoiceItemId,
            PackageName = p.PackageDefinition != null ? p.PackageDefinition.Name : "",
            ServiceName = p.PackageDefinition != null && p.PackageDefinition.Service != null ? p.PackageDefinition.Service.Name : "",
            ClientName = p.SlnClient != null ? p.SlnClient.FullName : null,
            TotalSessions = p.TotalSessions,
            UsedSessions = p.UsedSessions,
            RemainingSessions = p.RemainingSessions,
            PackagePrice = p.PackageDefinition != null ? p.PackageDefinition.Price : 0,
            SaleAmount = p.SaleAmount > 0 ? p.SaleAmount : (p.PackageDefinition != null ? p.PackageDefinition.Price : 0),
            PaidAmount = p.PaidAmount,
            BalanceAmount = (p.SaleAmount > 0 ? p.SaleAmount : (p.PackageDefinition != null ? p.PackageDefinition.Price : 0)) > p.PaidAmount
                ? (p.SaleAmount > 0 ? p.SaleAmount : (p.PackageDefinition != null ? p.PackageDefinition.Price : 0)) - p.PaidAmount
                : 0,
            ExpiresAt = p.ExpiresAt,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        }).ToListAsync();
    }

    public async Task<(SlnClientPackageDto? Package, string? Error)> AssignPackageAsync(SlnClientPackageAssignDto dto, int userId, int customerId, int? branchId = null)
    {
        return (null, "Seansli hizmet musteriye atama ile acilmaz. Hizli satis/adisyon uzerinden satildiginda seans takibi otomatik olusur.");
    }

    public async Task<(SlnClientPackageDto? Package, string? Error)> SellPackageAsync(SlnClientPackageSellDto dto, int userId, int customerId, int? branchId = null)
    {
        var def = await _defEs.GetAllQueryable()
            .FirstOrDefaultAsync(d => d.Id == dto.PackageDefinitionId && d.CustomerId == customerId);
        if (def == null) return (null, "Seans tanimi bulunamadi");
        if (!def.IsActive) return (null, "Seans tanimi aktif degil");
        if (!dto.SlnClientId.HasValue) return (null, "Seansli hizmet satisi icin musteri secilmelidir");

        var created = await CreateSessionPlansFromInvoiceAsync(
            customerId,
            dto.SlnClientId.Value,
            0,
            [new SlnSessionPlanSaleLine(def.ServiceId, def.Price, 1)],
            userId,
            branchId);

        return created.Count > 0
            ? (created[0], null)
            : (null, "Seansli hizmet satisi kaydedilemedi");
    }

    public async Task<List<SlnClientPackageDto>> CreateSessionPlansFromInvoiceAsync(int customerId, int slnClientId, int invoiceId, IEnumerable<SlnSessionPlanSaleLine> lines, int userId, int? branchId = null)
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
            .GroupBy(l => l.ServiceId)
            .Select(g => new
            {
                ServiceId = g.Key,
                Quantity = g.Sum(l => l.Quantity),
                PaidAmount = g.Sum(l => l.PaidAmount),
                InvoiceItemId = g.Select(l => l.InvoiceItemId).FirstOrDefault(id => id.HasValue)
            })
            .ToList();

        if (saleLines.Count == 0)
            return [];

        var serviceIds = saleLines.Select(l => l.ServiceId).Distinct().ToList();
        var definitions = await _defEs.GetAllQueryable()
            .Where(d => d.CustomerId == customerId
                && d.IsActive
                && d.TotalSessions > 0
                && serviceIds.Contains(d.ServiceId))
            .OrderByDescending(d => d.Id)
            .ToListAsync();

        var created = new List<SlnClientPackage>();
        foreach (var line in saleLines)
        {
            var def = definitions.FirstOrDefault(d => d.ServiceId == line.ServiceId);
            if (def == null)
                continue;

            var planCount = Math.Max(1, line.Quantity);
            var paidPerPlan = planCount > 0 ? Math.Round(line.PaidAmount / planCount, 2) : line.PaidAmount;
            for (var i = 0; i < planCount; i++)
            {
                var pkg = new SlnClientPackage
                {
                    CustomerId = customerId,
                    PackageDefinitionId = def.Id,
                    SlnClientId = slnClientId,
                    BranchId = branchId,
                    TotalSessions = def.TotalSessions,
                    UsedSessions = 0,
                    RemainingSessions = def.TotalSessions,
                    SaleAmount = def.Price,
                    PaidAmount = paidPerPlan,
                    SourceInvoiceId = invoiceId > 0 ? invoiceId : null,
                    SourceInvoiceItemId = line.InvoiceItemId,
                    ExpiresAt = DateTime.UtcNow.AddDays(def.ValidDays),
                    IsActive = true,
                    SoldByPersonnelId = userId
                };
                _pkgEs.Add(pkg);
                created.Add(pkg);
            }
        }

        if (created.Count == 0)
            return [];

        await _uow.SaveChangesAsync();

        var ids = created.Select(p => p.Id).ToList();
        return (await GetClientPackagesAsync(customerId, slnClientId, branchId))
            .Where(p => ids.Contains(p.Id))
            .ToList();
    }

    public async Task<(bool Success, string? Error)> UseSessionAsync(SlnPackageUseDto dto, int userId, int customerId, int? branchId = null)
    {
        return await RecordUsageAsync(customerId, dto.ClientPackageId, null, null, userId, dto.Notes, branchId);
    }

    public async Task<List<SlnPackageUsageDto>> GetUsageHistoryAsync(int customerId, int? clientPackageId = null, int? branchId = null)
    {
        var scopedPackageIds = SalonBranchScope.ApplyToClientPackages(
                _pkgEs.GetAllQueryable().Where(p => p.CustomerId == customerId),
                branchId)
            .Select(p => p.Id);

        var query = _usageEs.GetAllQueryable()
            .Where(u => scopedPackageIds.Contains(u.ClientPackageId));

        if (clientPackageId.HasValue)
            query = query.Where(u => u.ClientPackageId == clientPackageId.Value);

        return await query
            .Include(u => u.ClientPackage).ThenInclude(p => p!.PackageDefinition).ThenInclude(d => d!.Service)
            .Include(u => u.ClientPackage).ThenInclude(p => p!.SlnClient)
            .Include(u => u.Personnel).ThenInclude(p => p!.User)
            .OrderByDescending(u => u.UsedAt)
            .Select(u => new SlnPackageUsageDto
            {
                Id = u.Id,
                ClientPackageId = u.ClientPackageId,
                InvoiceId = u.InvoiceId,
                InvoiceItemId = u.InvoiceItemId,
                ServiceId = u.ServiceId,
                SlnAppointmentId = u.SlnAppointmentId,
                PackageName = u.ClientPackage != null && u.ClientPackage.PackageDefinition != null ? u.ClientPackage.PackageDefinition.Name : "",
                ServiceName = u.ClientPackage != null && u.ClientPackage.PackageDefinition != null && u.ClientPackage.PackageDefinition.Service != null ? u.ClientPackage.PackageDefinition.Service.Name : "",
                ClientName = u.ClientPackage != null && u.ClientPackage.SlnClient != null ? u.ClientPackage.SlnClient.FullName : null,
                PersonnelName = u.Personnel != null && u.Personnel.User != null ? u.Personnel.User.FullName : null,
                Notes = u.Notes,
                UsedAt = u.UsedAt
            })
            .ToListAsync();
    }

    public async Task<List<SlnPackageBenefitDto>> GetUsablePackagesAsync(int customerId, int slnClientId, IEnumerable<int> serviceIds, int? branchId = null)
    {
        var ids = serviceIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return [];

        var now = DateTime.UtcNow;
        return await SalonBranchScope.ApplyToClientPackages(
                _pkgEs.GetAllQueryable()
                    .Where(p => p.CustomerId == customerId
                        && p.SlnClientId == slnClientId
                        && p.IsActive
                        && p.RemainingSessions > 0
                        && (!p.ExpiresAt.HasValue || p.ExpiresAt.Value >= now)),
                branchId)
            .Include(p => p.PackageDefinition)
            .Where(p => p.PackageDefinition != null && ids.Contains(p.PackageDefinition.ServiceId))
            .OrderBy(p => p.ExpiresAt.HasValue ? 0 : 1)
            .ThenBy(p => p.ExpiresAt)
            .ThenBy(p => p.Id)
            .Select(p => new SlnPackageBenefitDto
            {
                ClientPackageId = p.Id,
                PackageDefinitionId = p.PackageDefinitionId,
                ServiceId = p.PackageDefinition!.ServiceId,
                PackageName = p.PackageDefinition.Name,
                RemainingSessions = p.RemainingSessions,
                ExpiresAt = p.ExpiresAt
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> RecordUsageAsync(int customerId, int clientPackageId, int? serviceId, int? slnClientId, int userId, string? notes, int? branchId = null, int? invoiceId = null, int? invoiceItemId = null, int? appointmentId = null)
    {
        var pkg = await SalonBranchScope.ApplyToClientPackages(
                _pkgEs.GetAllQueryable().Where(p => p.Id == clientPackageId && p.CustomerId == customerId),
                branchId)
            .Include(p => p.PackageDefinition)
            .FirstOrDefaultAsync();
        if (pkg == null) return (false, "Paket bulunamadi");
        if (!pkg.SlnClientId.HasValue) return (false, "Musteriye bagli olmayan paket kullanilamaz");
        if (slnClientId.HasValue && pkg.SlnClientId.Value != slnClientId.Value) return (false, "Paket secili musteriye ait degil");
        if (serviceId.HasValue && pkg.PackageDefinition?.ServiceId != serviceId.Value) return (false, "Paket bu hizmet icin kullanilamaz");
        if (!pkg.IsActive) return (false, "Bu paket aktif degil");
        if (pkg.RemainingSessions <= 0) return (false, "Kalan seans yok");
        if (pkg.ExpiresAt.HasValue && pkg.ExpiresAt.Value < DateTime.UtcNow) return (false, "Paketin suresi dolmus");

        pkg.UsedSessions++;
        pkg.RemainingSessions--;
        if (pkg.RemainingSessions == 0) pkg.IsActive = false;

        _usageEs.Add(new SlnPackageUsage
        {
            ClientPackageId = pkg.Id,
            PersonnelId = userId > 0 ? userId : null,
            InvoiceId = invoiceId,
            InvoiceItemId = invoiceItemId,
            ServiceId = serviceId,
            SlnAppointmentId = appointmentId,
            Notes = notes
        });

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ReverseInvoiceUsagesAsync(int customerId, int invoiceId)
    {
        var usages = await _usageEs.GetAllQueryable()
            .Include(u => u.ClientPackage)
            .Where(u => u.ClientPackage != null
                && u.ClientPackage.CustomerId == customerId
                && (u.InvoiceId == invoiceId
                    || (u.Notes != null && u.Notes.Contains($"Invoice:{invoiceId}|"))))
            .ToListAsync();

        foreach (var usage in usages)
        {
            var pkg = usage.ClientPackage!;
            pkg.UsedSessions = Math.Max(0, pkg.UsedSessions - 1);
            pkg.RemainingSessions = Math.Min(pkg.TotalSessions, pkg.RemainingSessions + 1);
            if (pkg.RemainingSessions > 0 && (!pkg.ExpiresAt.HasValue || pkg.ExpiresAt.Value >= DateTime.UtcNow))
                pkg.IsActive = true;
            _usageEs.Remove(usage);
        }

        if (usages.Count > 0)
            await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelPackageSaleFromInvoiceAsync(int customerId, string? invoiceNotes)
    {
        var packageIds = TryReadNoteInts(invoiceNotes, "SessionPlanSale:");
        var legacyPackageId = TryReadNoteInt(invoiceNotes, "PackageSale:");
        if (legacyPackageId.HasValue)
            packageIds.Add(legacyPackageId.Value);

        packageIds = packageIds.Distinct().ToList();
        if (packageIds.Count == 0)
            return (true, null);

        var packages = await _pkgEs.GetAllQueryable()
            .Where(p => packageIds.Contains(p.Id) && p.CustomerId == customerId)
            .ToListAsync();
        if (packages.Count == 0)
            return (true, null);

        if (packages.Any(p => p.UsedSessions > 0))
            return (false, "Kullanilmis seansli hizmet satisi iptal edilemez. Once manuel/pro-rata iade akisi uygulanmali.");

        foreach (var pkg in packages)
        {
            pkg.IsActive = false;
            pkg.RemainingSessions = 0;
            pkg.PaidAmount = 0;
        }
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static List<int> TryReadNoteInts(string? notes, string prefix)
    {
        var values = new List<int>();
        if (string.IsNullOrWhiteSpace(notes))
            return values;

        var index = notes.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return values;

        var start = index + prefix.Length;
        var end = start;
        while (end < notes.Length && (char.IsDigit(notes[end]) || notes[end] == ','))
            end++;

        foreach (var part in notes[start..end].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var value))
                values.Add(value);
        }

        return values;
    }

    private static int? TryReadNoteInt(string? notes, string prefix)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return null;

        var index = notes.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var start = index + prefix.Length;
        var end = start;
        while (end < notes.Length && char.IsDigit(notes[end]))
            end++;

        return end > start && int.TryParse(notes[start..end], out var value) ? value : null;
    }
}

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
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly ISlnInvoiceItemEntityService _invoiceItems;
    private readonly ISlnCashRegisterEntityService _cashRegisters;
    private readonly ISlnCashTransactionEntityService _cashTransactions;
    private readonly IUnitOfWork _uow;

    public SlnPackageFactory(
        ISlnPackageDefinitionEntityService defEs,
        ISlnClientPackageEntityService pkgEs,
        ISlnPackageUsageEntityService usageEs,
        ISlnClientEntityService clients,
        ISlnInvoiceEntityService invoices,
        ISlnInvoiceItemEntityService invoiceItems,
        ISlnCashRegisterEntityService cashRegisters,
        ISlnCashTransactionEntityService cashTransactions,
        IUnitOfWork uow)
    {
        _defEs = defEs;
        _pkgEs = pkgEs;
        _usageEs = usageEs;
        _clients = clients;
        _invoices = invoices;
        _invoiceItems = invoiceItems;
        _cashRegisters = cashRegisters;
        _cashTransactions = cashTransactions;
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
            PackageName = p.PackageDefinition != null ? p.PackageDefinition.Name : "",
            ServiceName = p.PackageDefinition != null && p.PackageDefinition.Service != null ? p.PackageDefinition.Service.Name : "",
            ClientName = p.SlnClient != null ? p.SlnClient.FullName : null,
            TotalSessions = p.TotalSessions,
            UsedSessions = p.UsedSessions,
            RemainingSessions = p.RemainingSessions,
            PaidAmount = p.PaidAmount,
            ExpiresAt = p.ExpiresAt,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        }).ToListAsync();
    }

    public async Task<(SlnClientPackageDto? Package, string? Error)> SellPackageAsync(SlnClientPackageSellDto dto, int userId, int customerId, int? branchId = null)
    {
        var def = await _defEs.GetAllQueryable().FirstOrDefaultAsync(d => d.Id == dto.PackageDefinitionId && d.CustomerId == customerId);
        if (def == null) return (null, "Paket tanimi bulunamadi");
        if (!def.IsActive) return (null, "Paket tanimi aktif degil");
        if (!dto.SlnClientId.HasValue) return (null, "Paket satisi icin musteri secilmelidir");
        if (def.TotalSessions <= 0) return (null, "Paket seans sayisi gecersiz");

        var clientExists = await SalonBranchScope.ApplyToClients(
                _clients.GetAllQueryable().Where(c => c.Id == dto.SlnClientId.Value && c.CustomerId == customerId),
                branchId)
            .AnyAsync();
        if (!clientExists) return (null, "Musteri bulunamadi");

        var pkg = new SlnClientPackage
        {
            CustomerId = customerId,
            PackageDefinitionId = def.Id,
            SlnClientId = dto.SlnClientId,
            TotalSessions = def.TotalSessions,
            UsedSessions = 0,
            RemainingSessions = def.TotalSessions,
            PaidAmount = def.Price,
            ExpiresAt = DateTime.UtcNow.AddDays(def.ValidDays),
            IsActive = true,
            SoldByPersonnelId = userId
        };
        _pkgEs.Add(pkg);
        await _uow.SaveChangesAsync();

        await CreatePackageSaleInvoiceAsync(customerId, branchId, userId, dto.PaymentMethodId, pkg, def);

        var result = (await GetClientPackagesAsync(customerId, null, branchId)).First(p => p.Id == pkg.Id);
        return (result, null);
    }

    public async Task<(bool Success, string? Error)> UseSessionAsync(SlnPackageUseDto dto, int userId, int customerId, int? branchId = null)
    {
        return await RecordUsageAsync(customerId, dto.ClientPackageId, null, null, userId, dto.Notes, branchId);
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

    public async Task<(bool Success, string? Error)> RecordUsageAsync(int customerId, int clientPackageId, int? serviceId, int? slnClientId, int userId, string? notes, int? branchId = null)
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
            Notes = notes
        });

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ReverseInvoiceUsagesAsync(int customerId, int invoiceId)
    {
        var token = $"Invoice:{invoiceId}|";
        var usages = await _usageEs.GetAllQueryable()
            .Include(u => u.ClientPackage)
            .Where(u => u.ClientPackage != null
                && u.ClientPackage.CustomerId == customerId
                && u.Notes != null
                && u.Notes.Contains(token))
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
        var packageId = TryReadNoteInt(invoiceNotes, "PackageSale:");
        if (!packageId.HasValue)
            return (true, null);

        var pkg = await _pkgEs.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == packageId.Value && p.CustomerId == customerId);
        if (pkg == null)
            return (true, null);

        if (pkg.UsedSessions > 0)
            return (false, "Kullanilmis paket satisi iptal edilemez. Once manuel/pro-rata iade akisi uygulanmali.");

        pkg.IsActive = false;
        pkg.RemainingSessions = 0;
        pkg.PaidAmount = 0;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private async Task CreatePackageSaleInvoiceAsync(
        int customerId,
        int? branchId,
        int userId,
        int paymentMethodId,
        SlnClientPackage pkg,
        SlnPackageDefinition def)
    {
        var today = DateTime.UtcNow;
        var todayCount = await _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId && i.InvoiceDate.Date == today.Date)
            .CountAsync();

        var invoiceNo = $"SLN-{today:yyyyMMdd}-{(todayCount + 1):D4}";
        var invoice = new SlnInvoice
        {
            CustomerId = customerId,
            BranchId = branchId,
            SlnClientId = pkg.SlnClientId,
            InvoiceNo = invoiceNo,
            InvoiceDate = today,
            TotalAmount = def.Price,
            NetAmount = def.Price,
            GrandTotal = def.Price,
            PaymentMethodId = paymentMethodId > 0 ? paymentMethodId : 1,
            PersonnelId = userId > 0 ? userId : null,
            StatusId = 2,
            Notes = $"PackageSale:{pkg.Id}|PackageDefinition:{def.Id}"
        };

        _invoices.Add(invoice);
        await _uow.SaveChangesAsync();

        _invoiceItems.Add(new SlnInvoiceItem
        {
            InvoiceId = invoice.Id,
            ServiceId = def.ServiceId,
            PersonnelId = userId > 0 ? userId : null,
            Quantity = 1,
            UnitPrice = def.Price,
            LineTotal = def.Price
        });
        await _uow.SaveChangesAsync();

        if (def.Price > 0)
        {
            var registerQuery = _cashRegisters.GetAllQueryable()
                .Where(r => r.CustomerId == customerId && r.IsActive);
            var register = branchId.HasValue
                ? await registerQuery.FirstOrDefaultAsync(r => r.BranchId == branchId.Value)
                  ?? await registerQuery.FirstOrDefaultAsync(r => r.BranchId == null)
                : await registerQuery.FirstOrDefaultAsync(r => r.BranchId == null)
                  ?? await registerQuery.FirstOrDefaultAsync();

            if (register != null)
            {
                _cashTransactions.Add(new SlnCashTransaction
                {
                    RegisterId = register.Id,
                    TransactionTypeId = 1,
                    Amount = def.Price,
                    PaymentMethodId = invoice.PaymentMethodId,
                    RelatedInvoiceId = invoice.Id,
                    Description = $"Paket satisi: {def.Name} ({invoiceNo})"
                });
                await _uow.SaveChangesAsync();
            }
        }
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

using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnDashboardFactory : ISlnDashboardFactory
{
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly ICustomerSubscriptionEntityService _subscriptions;
    private readonly ICustomerPortalModuleEntityService _portalModules;

    public SlnDashboardFactory(
        ISlnClientEntityService clients,
        ISlnAppointmentEntityService appointments,
        ISlnInvoiceEntityService invoices,
        ICustomerPersonnelEntityService personnel,
        ICustomerSubscriptionEntityService subscriptions,
        ICustomerPortalModuleEntityService portalModules)
    {
        _clients = clients;
        _appointments = appointments;
        _invoices = invoices;
        _personnel = personnel;
        _subscriptions = subscriptions;
        _portalModules = portalModules;
    }

    public async Task<object> GetDashboardAsync(int customerId, int? branchId = null)
    {
        var todayStart = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var todayEnd = todayStart.AddDays(1);

        // Toplam musteri (firma bazli)
        var totalClients = await _clients.GetAllQueryable()
            .CountAsync(c => c.CustomerId == customerId && !c.IsBlacklisted);

        // Bugunun randevulari (sube filtresi)
        var apptQuery = _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId
                     && a.StartTime >= todayStart
                     && a.StartTime < todayEnd
                     && a.StatusId != 4); // Iptal hariç

        if (branchId.HasValue)
            apptQuery = apptQuery.Where(a => a.BranchId == branchId.Value);

        var todayAppointments = await apptQuery
            .Include(a => a.SlnClient)
            .Include(a => a.Personnel).ThenInclude(p => p!.User)
            .OrderBy(a => a.StartTime)
            .Take(20)
            .Select(a => new
            {
                a.Id,
                clientName = a.SlnClient != null ? a.SlnClient.FullName : "-",
                personnelName = a.Personnel != null && a.Personnel.User != null ? a.Personnel.User.FullName : "-",
                startTime = a.StartTime, // ISO string client'a
                a.StatusId
            })
            .ToListAsync();

        var todayAppointmentsCount = await apptQuery.CountAsync();

        // Bugunun cirosu — odenmis (StatusId=2) adisyonlar
        var revQuery = _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId
                     && i.InvoiceDate >= todayStart
                     && i.InvoiceDate < todayEnd
                     && i.StatusId == 2);
        if (branchId.HasValue)
            revQuery = revQuery.Where(i => i.BranchId == branchId.Value);
        var todayRevenue = await revQuery.SumAsync(i => (decimal?)i.NetAmount) ?? 0m;

        // Aktif personel sayisi (sube bazli)
        var staffQuery = _personnel.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && p.IsActive);
        if (branchId.HasValue)
            staffQuery = staffQuery.Where(p => p.BranchId == branchId.Value);
        var activeStaff = await staffQuery.CountAsync();

        // Hatirlatmalar — bu hafta dogum gunu olan musteriler (TR-aware)
        var today = DateTime.UtcNow.Date;
        var weekEnd = today.AddDays(7);
        var clientsWithBd = await _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId && c.BirthDate.HasValue)
            .Select(c => new { c.Id, c.FullName, c.BirthDate })
            .ToListAsync();

        var birthdayReminders = clientsWithBd
            .Select(c =>
            {
                var bd = c.BirthDate!.Value;
                var thisYearBd = new DateTime(today.Year, bd.Month, bd.Day);
                if (thisYearBd < today) thisYearBd = thisYearBd.AddYears(1);
                return new { c.Id, c.FullName, bdDate = thisYearBd };
            })
            .Where(x => x.bdDate >= today && x.bdDate < weekEnd)
            .OrderBy(x => x.bdDate)
            .Take(5)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                bdDate = x.bdDate.ToString("dd MMM"),
                type = "birthday"
            })
            .ToList();

        // ═══ Abonelik bilgileri ═══
        var subscription = await _subscriptions.GetAllQueryable()
            .Where(s => s.CustomerId == customerId && s.StatusId != 3) // iptal haric, en son ak'i al
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        object? subscriptionInfo = null;
        if (subscription != null)
        {
            // Aktif modulleri al, gruplara gore paket fiyatlarini topla
            var activeModuleIds = await _portalModules.GetAllQueryable()
                .Where(m => m.CustomerId == customerId && m.IsActive)
                .Select(m => m.ModuleId)
                .ToListAsync();

            var activeGroupIds = activeModuleIds
                .Select(id => SalonModuleGroups.GetGroupId(id))
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .Distinct()
                .ToList();

            var activePackages = activeGroupIds
                .Select(gId => SalonModuleGroups.GetById(gId))
                .Where(p => p != null)
                .Select(p => new { id = p!.Id, name = p.Description, monthlyPrice = p.MonthlyPrice })
                .ToList();

            const decimal basicPackagePrice = 1700m;
            var monthlyTotal = basicPackagePrice + activePackages.Sum(p => p.monthlyPrice);

            var isTrial = subscription.MonthlyPrice == 0;
            int? trialDaysRemaining = null;
            if (isTrial)
            {
                var diff = (subscription.NextBillingDate.Date - DateTime.UtcNow.Date).Days;
                trialDaysRemaining = diff > 0 ? diff : 0;
            }

            subscriptionInfo = new
            {
                statusId = subscription.StatusId,
                isTrial,
                trialDaysRemaining,
                nextBillingDate = subscription.NextBillingDate,
                basicPackagePrice,
                activePackages,
                monthlyTotal
            };
        }

        return new
        {
            totalClients,
            todayAppointmentsCount,
            todayRevenue,
            activeStaff,
            todayAppointments,
            reminders = birthdayReminders,
            subscription = subscriptionInfo
        };
    }
}

using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class BillingFactory : IBillingFactory
{
    private readonly IBillingPeriodEntityService _billingEs;
    private readonly ICustomerEntityService _customerEs;
    private readonly ICustomerPersonnelEntityService _personnelEs;
    private readonly IUnitOfWork _uow;

    public BillingFactory(
        IBillingPeriodEntityService billingEs,
        ICustomerEntityService customerEs,
        ICustomerPersonnelEntityService personnelEs,
        IUnitOfWork uow)
    {
        _billingEs = billingEs;
        _customerEs = customerEs;
        _personnelEs = personnelEs;
        _uow = uow;
    }

    public async Task<List<BillingPeriodDto>> GetByCustomerAsync(int customerId)
    {
        return await _billingEs.GetAllQueryable()
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month)
            .Select(b => new BillingPeriodDto
            {
                Id = b.Id,
                CustomerId = b.CustomerId,
                CustomerName = b.Customer.Name,
                Year = b.Year,
                Month = b.Month,
                PeriodStartDate = b.PeriodStartDate,
                PeriodEndDate = b.PeriodEndDate,
                UserCount = b.UserCount,
                UnitPrice = b.UnitPrice,
                Amount = b.Amount,
                IsPaid = b.IsPaid,
                PaidAt = b.PaidAt,
                Notes = b.Notes
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpdatePeriodAsync(int periodId, BillingPeriodUpdateDto dto)
    {
        var period = await _billingEs.GetByIdAsync(periodId);
        if (period == null) return (false, "Faturalama donemi bulunamadi.");

        period.IsPaid = dto.IsPaid;
        period.PaidAt = dto.IsPaid ? DateTime.UtcNow : null;
        period.Notes = dto.Notes;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeletePeriodAsync(int periodId)
    {
        var period = await _billingEs.GetByIdAsync(periodId);
        if (period == null) return (false, "Faturalama donemi bulunamadi.");

        if (period.IsPaid)
            return (false, "Odemesi onaylanmis donem silinemez.");

        _billingEs.Remove(period);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(int Created, int Skipped, string? Error)> GenerateBulkAsync(int year, int month)
    {
        if (month < 1 || month > 12)
            return (0, 0, "Gecersiz ay degeri.");
        if (year < 2020 || year > 2100)
            return (0, 0, "Gecersiz yil degeri.");

        var activeCustomers = await _customerEs.GetAllQueryable()
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.MonthlyUnitPrice, c.CreatedAt })
            .ToListAsync();

        var existingCustomerIds = await _billingEs.GetAllQueryable()
            .Where(b => b.Year == year && b.Month == month)
            .Select(b => b.CustomerId)
            .ToListAsync();

        var created = 0;
        var skipped = 0;

        foreach (var customer in activeCustomers)
        {
            if (existingCustomerIds.Contains(customer.Id))
            {
                skipped++;
                continue;
            }

            // Donem baslangic gunu: musterinin olusturulma gunune gore
            var startDay = customer.CreatedAt.Day;
            var daysInMonth = DateTime.DaysInMonth(year, month);
            if (startDay > daysInMonth) startDay = daysInMonth;

            var periodStart = new DateTime(year, month, startDay, 0, 0, 0, DateTimeKind.Utc);

            // Donem bitis: bir sonraki ayin ayni gunu - 1 gun
            var nextMonth = month == 12 ? 1 : month + 1;
            var nextYear = month == 12 ? year + 1 : year;
            var daysInNextMonth = DateTime.DaysInMonth(nextYear, nextMonth);
            var endDay = Math.Min(startDay, daysInNextMonth);
            var periodEnd = new DateTime(nextYear, nextMonth, endDay, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);

            var userCount = await _personnelEs.GetAllQueryable()
                .CountAsync(p => p.CustomerId == customer.Id && p.IsActive
                    && p.CustomerRoleId == CustomerRoles.Ids.Operator);

            _billingEs.Add(new CustomerBillingPeriod
            {
                CustomerId = customer.Id,
                Year = year,
                Month = month,
                PeriodStartDate = periodStart,
                PeriodEndDate = periodEnd,
                UserCount = userCount,
                UnitPrice = customer.MonthlyUnitPrice,
                Amount = userCount * customer.MonthlyUnitPrice,
                IsPaid = false,
                CreatedAt = DateTime.UtcNow
            });

            created++;
        }

        if (created > 0)
            await _uow.SaveChangesAsync();

        return (created, skipped, null);
    }

    public async Task<(bool IsBlocked, string? Reason)> IsCustomerBlockedByBillingAsync(int customerId)
    {
        // Odenmemis donem var mi? Donem sonu + 3 gun gecmisse engelle.
        var now = DateTime.UtcNow;
        var unpaidPeriods = await _billingEs.GetAllQueryable()
            .Where(b => b.CustomerId == customerId && !b.IsPaid)
            .Select(b => new { b.Year, b.Month })
            .ToListAsync();

        foreach (var period in unpaidPeriods)
        {
            // Donemin son gunu + 3 gun
            var daysInMonth = DateTime.DaysInMonth(period.Year, period.Month);
            var deadline = new DateTime(period.Year, period.Month, daysInMonth, 23, 59, 59, DateTimeKind.Utc).AddDays(3);

            if (now > deadline)
                return (true, $"Odenmemis donem: {period.Month:00}/{period.Year}. Lutfen odeme yapiniz.");
        }

        return (false, null);
    }
}

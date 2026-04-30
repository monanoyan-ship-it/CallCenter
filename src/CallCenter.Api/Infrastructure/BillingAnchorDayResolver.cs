namespace CallCenter.Api.Infrastructure;

/// <summary>
/// Tahakkuk dönemi başlangıç günü. <see cref="Customer.BillingAnchorDay"/> yokken:
/// kesilen ay UTC bugün ile aynıysa bugünün günü (ayı aşmaz), değilse 1.
/// </summary>
public static class BillingAnchorDayResolver
{
    public static int ResolvePeriodStartDay(int year, int month, int? billingAnchorDay)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        if (billingAnchorDay.HasValue)
            return Math.Min(billingAnchorDay.Value, daysInMonth);

        var now = DateTime.UtcNow;
        if (now.Year == year && now.Month == month)
            return Math.Min(now.Day, daysInMonth);

        return 1;
    }
}

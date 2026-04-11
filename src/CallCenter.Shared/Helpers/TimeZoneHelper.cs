namespace CallCenter.Shared.Helpers;

/// <summary>
/// Timezone donusum helper.
/// IANA timezone ID'leri kullanir (Europe/Istanbul, Europe/Berlin vb.)
/// UTC tarih alir, belirtilen timezone'a cevirir veya tam tersi.
/// </summary>
public static class TimeZoneHelper
{
    /// <summary>UTC tarihini belirtilen timezone'a cevir</summary>
    public static DateTime ToLocal(DateTime utcDateTime, string timeZoneId)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), tz);
        }
        catch
        {
            // Gecersiz timezone — olduğu gibi dondur
            return utcDateTime;
        }
    }

    /// <summary>Yerel tarihini UTC'ye cevir</summary>
    public static DateTime ToUtc(DateTime localDateTime, string timeZoneId)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), tz);
        }
        catch
        {
            return localDateTime;
        }
    }

    /// <summary>Timezone gecerli mi?</summary>
    public static bool IsValid(string timeZoneId)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch { return false; }
    }

    /// <summary>Yaygin timezone listesi (UI dropdown icin)</summary>
    public static List<TimeZoneOption> GetCommonTimeZones() =>
    [
        new("Europe/Istanbul", "Türkiye (UTC+3)"),
        new("Europe/Berlin", "Almanya (UTC+1/+2)"),
        new("Europe/London", "İngiltere (UTC+0/+1)"),
        new("Europe/Paris", "Fransa (UTC+1/+2)"),
        new("Europe/Amsterdam", "Hollanda (UTC+1/+2)"),
        new("Europe/Vienna", "Avusturya (UTC+1/+2)"),
        new("Europe/Zurich", "İsviçre (UTC+1/+2)"),
        new("Europe/Stockholm", "İsveç (UTC+1/+2)"),
        new("Europe/Brussels", "Belçika (UTC+1/+2)"),
        new("Europe/Moscow", "Rusya - Moskova (UTC+3)"),
        new("Europe/Athens", "Yunanistan (UTC+2/+3)"),
        new("Europe/Bucharest", "Romanya (UTC+2/+3)"),
        new("Asia/Baku", "Azerbaycan (UTC+4)"),
        new("Asia/Tbilisi", "Gürcistan (UTC+4)"),
        new("Asia/Dubai", "BAE (UTC+4)"),
        new("Asia/Riyadh", "Suudi Arabistan (UTC+3)"),
        new("America/New_York", "ABD - Doğu (UTC-5/-4)"),
        new("America/Los_Angeles", "ABD - Batı (UTC-8/-7)"),
        new("America/Toronto", "Kanada - Doğu (UTC-5/-4)"),
        new("Australia/Sydney", "Avustralya (UTC+10/+11)"),
        new("Asia/Tokyo", "Japonya (UTC+9)"),
    ];
}

public record TimeZoneOption(string Id, string DisplayName);
